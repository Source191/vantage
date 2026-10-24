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

/// <summary>
/// What removing a worktree should do. CanBeForced says, for a refusal, whether force-remove would go ahead: true when
/// only work would be lost, false for a lock or a live session, so the view never has to read the reason to find out.
/// </summary>
public sealed record RemovalDecision(
    RemovalOutcome Outcome,
    bool Forced,
    BranchAction Branch,
    bool PruneRegistration,
    string Reason,
    bool CanBeForced)
{
    public static RemovalDecision For(WorktreeRemovalFacts facts, bool forced)
    {
        return facts switch
        {
            { LiveSessionName: not null } => Refused($"session {facts.LiveSessionName} is using it", canBeForced: false),
            { Health: WorktreeHealthKind.Locked } => Refused("the worktree is locked", canBeForced: false),
            { Health: WorktreeHealthKind.Empty } => DeleteFolder(forced, prune: false),
            { Health: WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken, LeftoverFileCount: > 0 } when !forced => Refused($"{LeftoverFiles(facts)} files that git no longer tracks would be lost", canBeForced: true),
            { Health: WorktreeHealthKind.Broken } => DeleteFolder(forced, prune: true),
            { Health: WorktreeHealthKind.Orphan } => DeleteFolder(forced, prune: false),
            { DirtyFileCount: > 0 } when !forced => Refused($"{facts.DirtyFileCount} changed or untracked files would be lost", canBeForced: true),
            { UnpushedCommitCount: > 0 } when !forced => Refused($"branch {facts.Branch} has {facts.UnpushedCommitCount} unpushed commits", canBeForced: true),
            _ => new(RemovalOutcome.RemoveWorktree, forced, BranchActionFor(facts), PruneRegistration: false, string.Empty, CanBeForced: false),
        };
    }

    private static RemovalDecision DeleteFolder(bool forced, bool prune)
    {
        return new(RemovalOutcome.DeleteFolder, forced, BranchAction.None, PruneRegistration: prune, "leftover folder", CanBeForced: false);
    }

    private static RemovalDecision Refused(string reason, bool canBeForced)
    {
        return new(RemovalOutcome.Refused, Forced: false, BranchAction.None, PruneRegistration: false, reason, canBeForced);
    }

    private static string LeftoverFiles(WorktreeRemovalFacts facts)
    {
        return facts.LeftoverFileCount >= WorktreeRemovalFacts.LeftoverFileLimit
            ? $"at least {WorktreeRemovalFacts.LeftoverFileLimit}"
            : facts.LeftoverFileCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static BranchAction BranchActionFor(WorktreeRemovalFacts facts)
    {
        return facts is { BranchCreatedByVantage: true, BranchMerged: true } ? BranchAction.Delete : BranchAction.Ask;
    }
}