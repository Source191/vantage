namespace Source191.Vantage.Domain;

/// <summary>Where a new worktree goes: <c>&lt;worktree root&gt;\&lt;repository&gt;-&lt;slug&gt;</c>, on a branch named after the slug.</summary>
public sealed record PlannedWorktree(FolderPath Path, BranchName Branch)
{
    public static PlannedWorktree For(FolderPath worktreeRoot, FolderPath repository, WorktreeSlug slug) => new PlannedWorktree(worktreeRoot.Combine($"{repository.Name}-{slug.Value}"), new BranchName(slug.Value));
}