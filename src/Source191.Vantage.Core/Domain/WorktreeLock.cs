namespace Source191.Vantage.Domain;

/// <summary>Whether git has a worktree locked, and with what reason: empty when the lock was given none.</summary>
public abstract record WorktreeLock
{
    public sealed record Unlocked : WorktreeLock;

    public sealed record Locked(string Reason) : WorktreeLock;
}