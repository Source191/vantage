namespace Source191.Vantage.Application;

/// <summary>When a worktree last committed, or unknown: git failed, the branch has no commits yet, or the folder could not be counted.</summary>
public abstract record LastCommit
{
    public sealed record Made(DateTimeOffset At) : LastCommit;

    public sealed record Unknown : LastCommit;
}