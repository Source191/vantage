namespace Source191.Vantage.Domain;

/// <summary>
/// What removing a worktree should do. CanBeForced says, for a refusal, whether force-remove would go ahead: true when
/// only work would be lost, false for a lock or a live session, so the view never has to read the reason to find out.
/// </summary>
public sealed record RemovalDecision(
    RemovalOutcome Outcome,
    bool Forced,
    BranchAction Branch,
    bool PruneRegistration,
    RemovalReason Reason,
    bool CanBeForced)
{
    public static RemovalDecision For(WorktreeRemovalFacts facts, bool forced)
    {
        return facts switch
        {
            { Session: WorktreeSession.Running running } => Refused(new RemovalReason.LiveSession(running.Name), canBeForced: false),
            { Health: WorktreeHealthKind.Locked } => Refused(new RemovalReason.Locked(), canBeForced: false),
            { Health: WorktreeHealthKind.Elsewhere } => Refused(new RemovalReason.RepositoryOutsideRoots(), canBeForced: false),
            { Health: WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken or WorktreeHealthKind.Empty, LeftoverFileCount: > 0 } when !forced => Refused(new RemovalReason.LeftoverFilesWouldBeLost(facts.LeftoverFileCount, AtLeast: facts.LeftoverFileCount >= WorktreeRemovalFacts.LeftoverFileLimit), canBeForced: true),
            { Health: WorktreeHealthKind.Empty } => DeleteFolder(forced, prune: false),
            { Health: WorktreeHealthKind.Broken } => DeleteFolder(forced, prune: true),
            { Health: WorktreeHealthKind.Orphan } => DeleteFolder(forced, prune: false),
            { DirtyFileCount: > 0 } when !forced => Refused(new RemovalReason.ChangesWouldBeLost(facts.DirtyFileCount), canBeForced: true),
            { UnpushedCommitCount: > 0 } when !forced => Refused(new RemovalReason.UnpushedCommits(facts.Branch, facts.UnpushedCommitCount), canBeForced: true),
            _ => new(RemovalOutcome.RemoveWorktree, forced, BranchActionFor(facts), PruneRegistration: false, new RemovalReason.None(), CanBeForced: false),
        };
    }

    private static RemovalDecision DeleteFolder(bool forced, bool prune) => new(RemovalOutcome.DeleteFolder, forced, BranchAction.None, PruneRegistration: prune, new RemovalReason.LeftoverFolder(), CanBeForced: false);

    private static RemovalDecision Refused(RemovalReason reason, bool canBeForced) => new(RemovalOutcome.Refused, Forced: false, BranchAction.None, PruneRegistration: false, reason, canBeForced);

    private static BranchAction BranchActionFor(WorktreeRemovalFacts facts) => facts is { BranchCreatedByVantage: true, BranchMerged: true } ? BranchAction.Delete : BranchAction.Ask;
}