namespace Source191.Torni.Domain;

/// <summary>
/// What removing a worktree should do: refuse, remove it through git, or delete a leftover folder. Each case holds
/// only what applies to it.
/// </summary>
public abstract record RemovalDecision
{
    /// <summary>
    /// Nothing is removed. CanBeForced says whether force-remove would go ahead: true when only work would be lost,
    /// false for a lock, a live session or a repository outside the roots, so the view never has to read the reason
    /// to find out.
    /// </summary>
    public sealed record Refused(RemovalReason Reason, bool CanBeForced) : RemovalDecision;

    /// <summary>The worktree goes through git, then its branch is dealt with as Branch says.</summary>
    public sealed record RemoveWorktree(bool Forced, BranchAction Branch) : RemovalDecision;

    /// <summary>Only the folder is deleted, because git no longer has the worktree; Prune also clears its registration.</summary>
    public sealed record DeleteFolder(bool Forced, bool Prune) : RemovalDecision;

    public static RemovalDecision For(WorktreeRemovalFacts facts, bool forced)
    {
        return facts switch
        {
            { Session: WorktreeSession.Running running } => new Refused(new RemovalReason.LiveSession(running.Name), CanBeForced: false),
            { Health: WorktreeHealthKind.Locked } => new Refused(new RemovalReason.Locked(), CanBeForced: false),
            { Health: WorktreeHealthKind.Elsewhere } => new Refused(new RemovalReason.RepositoryOutsideRoots(), CanBeForced: false),
            { Health: WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken or WorktreeHealthKind.Empty, LeftoverFileCount: > 0 } when !forced => new Refused(new RemovalReason.LeftoverFilesWouldBeLost(facts.LeftoverFileCount, AtLeast: facts.LeftoverFileCount >= WorktreeRemovalFacts.LeftoverFileLimit), CanBeForced: true),
            { Health: WorktreeHealthKind.Empty } => new DeleteFolder(forced, Prune: false),
            { Health: WorktreeHealthKind.Broken } => new DeleteFolder(forced, Prune: true),
            { Health: WorktreeHealthKind.Orphan } => new DeleteFolder(forced, Prune: false),
            { DirtyFileCount: > 0 } when !forced => new Refused(new RemovalReason.ChangesWouldBeLost(facts.DirtyFileCount), CanBeForced: true),
            { UnpushedCommitCount: > 0 } when !forced => new Refused(new RemovalReason.UnpushedCommits(facts.Branch, facts.UnpushedCommitCount), CanBeForced: true),
            _ => new RemoveWorktree(forced, BranchActionFor(facts)),
        };
    }

    private static BranchAction BranchActionFor(WorktreeRemovalFacts facts) => facts is { BranchCreatedByTorni: true, BranchMerged: true } ? BranchAction.Delete : BranchAction.Ask;
}