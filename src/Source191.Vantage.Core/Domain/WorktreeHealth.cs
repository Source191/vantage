namespace Source191.Vantage.Domain;

public enum WorktreeHealthKind
{
    Ok,
    Locked,
    Orphan,
    Broken,
    Empty,
}

public sealed record WorktreeHealth(WorktreeHealthKind Kind, string Reason)
{
    public static WorktreeHealth From(WorktreeObservation observation)
    {
        return observation switch
        {
            { RegisteredWithGit: true, FolderExists: false } => new(WorktreeHealthKind.Broken, "registered with git but the folder is gone"),
            { RegisteredWithGit: true, LockReason: not null } => new(WorktreeHealthKind.Locked, observation.LockReason),
            { RegisteredWithGit: true } => new(WorktreeHealthKind.Ok, string.Empty),
            { GitFileTargetExists: false } => new(WorktreeHealthKind.Broken, "its .git file points at a registration that no longer exists"),
            { UnderClaudeWorktrees: true } => new(WorktreeHealthKind.Orphan, "under .claude\\worktrees but git no longer lists it"),
            { EmptyLeftoverFolder: true } => new(WorktreeHealthKind.Empty, "leftover folder with nothing in it"),
            _ => new(WorktreeHealthKind.Orphan, "folder is not registered with git"),
        };
    }
}