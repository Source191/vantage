namespace Source191.Vantage.Domain;

public sealed record WorktreeRemovalFacts(
    WorktreeHealthKind Health,
    string Branch,
    int DirtyFileCount,
    int UnpushedCommitCount,
    string? LiveSessionName,
    bool BranchCreatedByVantage,
    bool BranchMerged,
    int LeftoverFileCount);