namespace Source191.Vantage.Domain;

public enum WorktreeHealthKind
{
    Ok,
    Locked,
    Orphan,
    Broken,
    Empty,

    /// <summary>A live worktree of a repository Vantage did not scan, because it is not under the roots.</summary>
    Elsewhere,
}

public sealed record WorktreeHealth(WorktreeHealthKind Kind, HealthReason Reason)
{
    public static WorktreeHealth From(WorktreeObservation observation)
    {
        return observation switch
        {
            { RegisteredWithGit: true, FolderExists: false } => new(WorktreeHealthKind.Broken, new HealthReason.FolderGone()),
            { RegisteredWithGit: true, Prunable: true } => new(WorktreeHealthKind.Broken, new HealthReason.GitFileGone()),
            { RegisteredWithGit: true, Lock: WorktreeLock.Locked locked } => new(WorktreeHealthKind.Locked, new HealthReason.Locked(locked.Reason)),
            { RegisteredWithGit: true } => new(WorktreeHealthKind.Ok, new HealthReason.None()),
            { GitFile: GitPointer.RegisteredElsewhere elsewhere } => new(WorktreeHealthKind.Elsewhere, new HealthReason.RepositoryOutsideRoots(elsewhere.Owner)),
            { GitFile: GitPointer.TargetGone } => new(WorktreeHealthKind.Broken, new HealthReason.RegistrationGone()),
            { UnderClaudeWorktrees: true } => new(WorktreeHealthKind.Orphan, new HealthReason.UnderClaudeWorktrees()),
            { EmptyLeftoverFolder: true } => new(WorktreeHealthKind.Empty, new HealthReason.EmptyLeftover()),
            _ => new(WorktreeHealthKind.Orphan, new HealthReason.NotRegistered()),
        };
    }
}