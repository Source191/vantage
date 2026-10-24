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
    string DescriptionText,
    LaunchMode Mode,
    WorktreeBase Base,
    string OpeningPromptText);

/// <summary>What the launch service will do: where the session starts, under what name, and whether a worktree is made first.</summary>
public sealed record LaunchPlan(
    FolderPath Directory,
    SessionName Name,
    string? Description,
    SiblingWorktree? Worktree,
    bool ReuseExistingWorktree,
    WorktreeBase Base,
    string? OpeningPrompt,
    RepositoryChoice Repository);

public sealed record LaunchPlanResult(LaunchPlan? Plan, string? Problem);

public static class LaunchPlanBuilder
{
    public static LaunchPlanResult Build(LaunchRequest request, Func<string, bool> folderExists)
    {
        SessionName name;
        try
        {
            name = SessionName.Parse(request.NameText);
        }
        catch (ArgumentException invalid)
        {
            return new LaunchPlanResult(Plan: null, invalid.Message);
        }

        string? description = Blank(request.DescriptionText) ? null : request.DescriptionText.Trim();
        string? prompt = Blank(request.OpeningPromptText) ? null : request.OpeningPromptText.Trim();

        if (request.Mode == LaunchMode.CurrentBranch)
        {
            return Ok(new LaunchPlan(request.Repository.Path, name, description, Worktree: null, ReuseExistingWorktree: false, request.Base, prompt, request.Repository));
        }

        if (!request.Repository.IsRepository)
        {
            return new LaunchPlanResult(Plan: null, "A worktree needs a repository; choose one or start on the current branch.");
        }

        if (name.Slug is null)
        {
            return new LaunchPlanResult(Plan: null, "A worktree needs a name with some letters or digits in it.");
        }

        SiblingWorktree worktree = SiblingWorktree.For(request.Repository.Path, name.Slug);
        bool reuse = folderExists(worktree.Path.Value);

        return Ok(new LaunchPlan(worktree.Path, name, description, worktree, reuse, request.Base, prompt, request.Repository));
    }

    private static LaunchPlanResult Ok(LaunchPlan plan)
    {
        return new LaunchPlanResult(plan, Problem: null);
    }

    private static bool Blank(string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }
}