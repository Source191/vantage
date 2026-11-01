namespace Source191.Vantage.Domain;

/// <summary>Whether a tab's shell has claimed its registration yet, and when it has, which process it is and when that process started.</summary>
public abstract record TabClaim
{
    /// <summary>Opened but not yet claimed: live for a short grace period while the tab starts.</summary>
    public sealed record Pending : TabClaim;

    /// <summary>Claimed by the tab's shell; live while a process with that id and start time runs.</summary>
    public sealed record Claimed(int ProcessId, DateTimeOffset StartedAt) : TabClaim;
}