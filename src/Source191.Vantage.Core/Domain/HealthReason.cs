namespace Source191.Vantage.Domain;

/// <summary>What is wrong with a worktree, as a fact; <c>Wording</c> turns each case into the words shown.</summary>
public abstract record HealthReason
{
    /// <summary>Nothing to explain.</summary>
    public sealed record None : HealthReason;

    public sealed record FolderGone : HealthReason;

    /// <summary>Git's own words for the lock, kept as it said them; they may be empty.</summary>
    public sealed record Locked(string LockReason) : HealthReason;

    public sealed record RegistrationGone : HealthReason;

    public sealed record UnderClaudeWorktrees : HealthReason;

    public sealed record EmptyLeftover : HealthReason;

    public sealed record NotRegistered : HealthReason;
}