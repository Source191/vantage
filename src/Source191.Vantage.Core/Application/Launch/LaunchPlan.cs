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

/// <summary>What the launch service will do: where the session starts, under what name, and whether a worktree is made first (<see cref="LaunchPlace"/>).</summary>
public sealed record LaunchPlan(
    FolderPath Directory,
    SessionName Name,
    LaunchPlace Place,
    WorktreeBase Base,
    OpeningPrompt OpeningPrompt,
    RepositoryChoice Repository)
{
    public PlannedWorktree? Worktree => Place switch
    {
        LaunchPlace.NewWorktree fresh => fresh.Worktree,
        LaunchPlace.ExistingWorktree existing => existing.Worktree,
        _ => null,
    };

    public bool ReuseExistingWorktree => Place is LaunchPlace.ExistingWorktree;
}

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
        OpeningPrompt prompt = Blank(request.OpeningPromptText) ? new OpeningPrompt.None() : new OpeningPrompt.Text(request.OpeningPromptText.Trim());
        if (prompt is OpeningPrompt.Text { Words: string text } && text.StartsWith('-'))
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.PromptLooksLikeAnOption());
        }

        if (request.Mode == LaunchMode.CurrentBranch)
        {
            return new LaunchPlanResult.Planned(new LaunchPlan(request.Repository.Path, name, new LaunchPlace.CurrentBranch(), request.Base, prompt, request.Repository));
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
        LaunchPlace place = folderExists(worktree.Path.Value) ? new LaunchPlace.ExistingWorktree(worktree) : new LaunchPlace.NewWorktree(worktree);

        return new LaunchPlanResult.Planned(new LaunchPlan(worktree.Path, name, place, request.Base, prompt, request.Repository));
    }

    private static bool Blank(string text) => string.IsNullOrWhiteSpace(text);
}