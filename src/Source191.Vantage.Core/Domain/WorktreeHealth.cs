namespace Source191.Vantage.Domain;

public enum WorktreeHealthKind
{
    Ok,
    Locked,
    Orphan,
    Broken,
    Empty,
}

public sealed record WorktreeHealth(WorktreeHealthKind Kind, HealthReason Reason)
{
    public static WorktreeHealth From(WorktreeObservation observation)
    {
        return observation switch
        {
            { RegisteredWithGit: true, FolderExists: false } => new(WorktreeHealthKind.Broken, new HealthReason.FolderGone()),
            { RegisteredWithGit: true, LockReason: not null } => new(WorktreeHealthKind.Locked, new HealthReason.Locked(observation.LockReason)),
            { RegisteredWithGit: true } => new(WorktreeHealthKind.Ok, new HealthReason.None()),
            { GitFileTargetExists: false } => new(WorktreeHealthKind.Broken, new HealthReason.RegistrationGone()),
            { UnderClaudeWorktrees: true } => new(WorktreeHealthKind.Orphan, new HealthReason.UnderClaudeWorktrees()),
            { EmptyLeftoverFolder: true } => new(WorktreeHealthKind.Empty, new HealthReason.EmptyLeftover()),
            _ => new(WorktreeHealthKind.Orphan, new HealthReason.NotRegistered()),
        };
    }
}