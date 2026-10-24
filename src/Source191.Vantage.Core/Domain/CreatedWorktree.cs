namespace Source191.Vantage.Domain;

/// <summary>A worktree and branch that Vantage created, so removal knows the branch is its own to delete.</summary>
public sealed record CreatedWorktree(FolderPath Path, BranchName Branch, FolderPath Repository, DateTimeOffset CreatedAt);