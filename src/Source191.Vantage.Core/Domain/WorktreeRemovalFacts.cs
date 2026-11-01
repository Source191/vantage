namespace Source191.Vantage.Domain;

/// <summary>
/// What <see cref="RemovalDecision.For"/> needs to know about a worktree to decide how removing it goes.
/// LeftoverFileCount is counted up to <see cref="LeftoverFileLimit"/> and no further, so a scan never walks a whole
/// dependencies tree; a count at the limit means "at least this many".
/// </summary>
public sealed record WorktreeRemovalFacts(
    WorktreeHealthKind Health,
    string Branch,
    int DirtyFileCount,
    int UnpushedCommitCount,
    WorktreeSession Session,
    bool BranchCreatedByVantage,
    bool BranchMerged,
    int LeftoverFileCount)
{
    public const int LeftoverFileLimit = 1000;
}