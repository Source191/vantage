using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Launch;

/// <summary>What the user typed and chose in the wizard, before any rule is applied.</summary>
public sealed record LaunchRequest(
    RepositoryChoice Repository,
    string NameText,
    LaunchMode Mode,
    WorktreeBase Base,
    string OpeningPromptText);

/// <summary>What the launch service will do: where the session starts, under what name, and whether a worktree is made first.</summary>
public sealed record LaunchPlan(
    FolderPath Directory,
    SessionName Name,
    PlannedWorktree? Worktree,
    bool ReuseExistingWorktree,
    WorktreeBase Base,
    string? OpeningPrompt,
    RepositoryChoice Repository);

/// <summary>Why a launch request is refused, as a fact; <see cref="Wording"/> says it, in the wizard and from vantage here.</summary>
public abstract record LaunchProblem
{
    public sealed record InvalidName(NameProblem Problem) : LaunchProblem;

    public sealed record WorktreeNeedsRepository : LaunchProblem;

    public sealed record WorktreeNeedsLetters : LaunchProblem;

    /// <summary>The opening prompt starts with a dash, which the CLI reads as options and has no -- to stop.</summary>
    public sealed record PromptLooksLikeAnOption : LaunchProblem;
}

/// <summary>A plan, or the reason there is none.</summary>
public abstract record LaunchPlanResult
{
    public sealed record Planned(LaunchPlan Plan) : LaunchPlanResult;

    public sealed record Refused(LaunchProblem Problem) : LaunchPlanResult;
}

public static class LaunchPlanBuilder
{
    public static LaunchPlanResult Build(LaunchRequest request, FolderPath worktreeRoot, Func<string, bool> folderExists)
    {
        return SessionName.Validate(request.NameText) switch
        {
            SessionNameValidation.Valid valid => Build(request, valid.Name, worktreeRoot, folderExists),
            SessionNameValidation.Refused refused => new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(refused.Problem)),
            _ => throw new UnreachableException(),
        };
    }

    private static LaunchPlanResult Build(LaunchRequest request, SessionName name, FolderPath worktreeRoot, Func<string, bool> folderExists)
    {
        string? prompt = Blank(request.OpeningPromptText) ? null : request.OpeningPromptText.Trim();
        if (prompt is not null && prompt.StartsWith('-'))
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.PromptLooksLikeAnOption());
        }

        if (request.Mode == LaunchMode.CurrentBranch)
        {
            return new LaunchPlanResult.Planned(new LaunchPlan(request.Repository.Path, name, Worktree: null, ReuseExistingWorktree: false, request.Base, prompt, request.Repository));
        }

        if (!request.Repository.IsRepository)
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsRepository());
        }

        if (!WorktreeSlug.TryParse(name.Value, out WorktreeSlug? slug))
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsLetters());
        }

        PlannedWorktree worktree = PlannedWorktree.For(worktreeRoot, request.Repository.Path, slug);
        bool reuse = folderExists(worktree.Path.Value);

        return new LaunchPlanResult.Planned(new LaunchPlan(worktree.Path, name, worktree, reuse, request.Base, prompt, request.Repository));
    }

    private static bool Blank(string text) => string.IsNullOrWhiteSpace(text);
}