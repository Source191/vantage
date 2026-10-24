namespace Source191.Vantage.Domain;

/// <summary>Where a new worktree's branch starts: origin's default branch, or whatever the repository has checked out.</summary>
public enum WorktreeBase
{
    RemoteDefault,
    Head,
}