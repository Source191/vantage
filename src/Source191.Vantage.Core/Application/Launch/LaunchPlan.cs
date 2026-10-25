using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

public enum LaunchMode
{
    CurrentBranch,
    NewWorktree,
}

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
    SiblingWorktree? Worktree,
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

    /// <summary>vantage here: an option was given without its value.</summary>
    public sealed record OptionNeedsValue(string Option) : LaunchProblem;

    public sealed record UnknownOption(string Option) : LaunchProblem;

    public sealed record UnexpectedArgument(string Argument) : LaunchProblem;
}

/// <summary>A plan, or the reason there is none.</summary>
public sealed record LaunchPlanResult(LaunchPlan? Plan, LaunchProblem? Problem)
{
    public static LaunchPlanResult Refused(LaunchProblem problem) => new LaunchPlanResult(Plan: null, problem);
}

public static class LaunchPlanBuilder
{
    public static LaunchPlanResult Build(LaunchRequest request, Func<string, bool> folderExists)
    {
        return SessionName.Validate(request.NameText) switch
        {
            SessionNameValidation.Valid valid => Build(request, valid.Name, folderExists),
            SessionNameValidation.Refused refused => LaunchPlanResult.Refused(new LaunchProblem.InvalidName(refused.Problem)),
            _ => LaunchPlanResult.Refused(new LaunchProblem.InvalidName(NameProblem.Missing)),
        };
    }

    private static LaunchPlanResult Build(LaunchRequest request, SessionName name, Func<string, bool> folderExists)
    {
        string? prompt = Blank(request.OpeningPromptText) ? null : request.OpeningPromptText.Trim();

        if (request.Mode == LaunchMode.CurrentBranch)
        {
            return Ok(new LaunchPlan(request.Repository.Path, name, Worktree: null, ReuseExistingWorktree: false, request.Base, prompt, request.Repository));
        }

        if (!request.Repository.IsRepository)
        {
            return LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsRepository());
        }

        if (name.Slug is null)
        {
            return LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsLetters());
        }

        SiblingWorktree worktree = SiblingWorktree.For(request.Repository.Path, name.Slug);
        bool reuse = folderExists(worktree.Path.Value);

        return Ok(new LaunchPlan(worktree.Path, name, worktree, reuse, request.Base, prompt, request.Repository));
    }

    private static LaunchPlanResult Ok(LaunchPlan plan) => new LaunchPlanResult(plan, Problem: null);

    private static bool Blank(string text) => string.IsNullOrWhiteSpace(text);
}