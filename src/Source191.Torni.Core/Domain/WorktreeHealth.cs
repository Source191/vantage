using System.Diagnostics;

namespace Source191.Torni.Domain;

public sealed record WorktreeHealth(WorktreeHealthKind Kind, HealthReason Reason)
{
    public static WorktreeHealth From(WorktreeObservation observation)
    {
        return observation switch
        {
            WorktreeObservation.Registered registered => FromRegistered(registered),
            WorktreeObservation.Unregistered unregistered => FromUnregistered(unregistered),
            _ => throw new UnreachableException(),
        };
    }

    private static WorktreeHealth FromRegistered(WorktreeObservation.Registered observation)
    {
        return observation switch
        {
            { FolderExists: false } => new(WorktreeHealthKind.Broken, new HealthReason.FolderGone()),
            { Prunable: true } => new(WorktreeHealthKind.Broken, new HealthReason.GitFileGone()),
            { Lock: WorktreeLock.Locked locked } => new(WorktreeHealthKind.Locked, new HealthReason.Locked(locked.Reason)),
            _ => new(WorktreeHealthKind.Ok, new HealthReason.None()),
        };
    }

    private static WorktreeHealth FromUnregistered(WorktreeObservation.Unregistered observation)
    {
        return observation switch
        {
            { GitFile: GitPointer.RegisteredElsewhere elsewhere } => new(WorktreeHealthKind.Elsewhere, new HealthReason.RepositoryOutsideRoots(elsewhere.Owner)),
            { GitFile: GitPointer.TargetGone } => new(WorktreeHealthKind.Broken, new HealthReason.RegistrationGone()),
            { UnderClaudeWorktrees: true } => new(WorktreeHealthKind.Orphan, new HealthReason.UnderClaudeWorktrees()),
            { EmptyLeftover: true } => new(WorktreeHealthKind.Empty, new HealthReason.EmptyLeftover()),
            _ => new(WorktreeHealthKind.Orphan, new HealthReason.NotRegistered()),
        };
    }
}