namespace Source191.Vantage.Domain;

public enum RemovalOutcome
{
    Refused,
    RemoveWorktree,
    DeleteFolder,
}

public enum BranchAction
{
    None,
    Delete,
    Ask,
}

public sealed record RemovalDecision(
    RemovalOutcome Outcome,
    bool Forced,
    BranchAction Branch,
    bool PruneRegistration,
    string Reason)
{
    public static RemovalDecision For(WorktreeRemovalFacts facts, bool forced)
    {
        return facts switch
        {
            { LiveSessionName: not null } => Refused($"session {facts.LiveSessionName} is using it"),
            { Health: WorktreeHealthKind.Locked } => Refused("the worktree is locked"),
            { Health: WorktreeHealthKind.Empty } => DeleteFolder(forced, prune: false),
            { Health: WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken, LeftoverFileCount: > 0 } when !forced => Refused($"{facts.LeftoverFileCount} files that git no longer tracks would be lost"),
            { Health: WorktreeHealthKind.Broken } => DeleteFolder(forced, prune: true),
            { Health: WorktreeHealthKind.Orphan } => DeleteFolder(forced, prune: false),
            { DirtyFileCount: > 0 } when !forced => Refused($"{facts.DirtyFileCount} changed or untracked files would be lost"),
            { UnpushedCommitCount: > 0 } when !forced => Refused($"branch {facts.Branch} has {facts.UnpushedCommitCount} unpushed commits"),
            _ => new(RemovalOutcome.RemoveWorktree, forced, BranchActionFor(facts), PruneRegistration: false, string.Empty),
        };
    }

    private static RemovalDecision DeleteFolder(bool forced, bool prune)
    {
        return new(RemovalOutcome.DeleteFolder, forced, BranchAction.None, PruneRegistration: prune, "leftover folder");
    }

    private static RemovalDecision Refused(string reason)
    {
        return new(RemovalOutcome.Refused, Forced: false, BranchAction.None, PruneRegistration: false, reason);
    }

    private static BranchAction BranchActionFor(WorktreeRemovalFacts facts)
    {
        return facts is { BranchCreatedByVantage: true, BranchMerged: true } ? BranchAction.Delete : BranchAction.Ask;
    }
}