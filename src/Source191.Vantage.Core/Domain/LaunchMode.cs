namespace Source191.Vantage.Domain;

/// <summary>Where a new session starts: the repository's checkout on whatever branch it has, or a new worktree on a branch of its own.</summary>
public enum LaunchMode
{
    CurrentBranch,
    NewWorktree,
}
