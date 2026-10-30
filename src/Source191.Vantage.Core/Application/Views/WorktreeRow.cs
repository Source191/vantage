using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// One line of the worktrees table: a linked worktree or a leftover folder, with everything the removal rules need
/// except the two facts only the state store and git can add (whether Vantage created the branch, and whether it is merged).
/// Repository is the owning main checkout (for a leftover whose owner is gone, where it used to be; for an empty
/// leftover, the root). OwnerExists says whether that repository still has a .git folder, so a stale registration can be pruned.
/// </summary>
public sealed record WorktreeRow(
    FolderPath Repository,
    FolderPath Path,
    BranchName? Branch,
    WorktreeHealth Health,
    int DirtyFiles,
    int UnpushedCommits,
    DateTimeOffset? LastCommitAt,
    SessionId? SessionId,
    string? SessionName,
    int LeftoverFileCount,
    bool OwnerExists,
    bool FolderExists)
{
    /// <summary>The branch's pull request, or why there is none; a leftover has no branch to look up.</summary>
    public PullRequestLookup PullRequest { get; init; } = new PullRequestLookup.Missing(PullRequestGap.NoBranch);

    public string RepositoryName => Repository.Name;

    public string FolderName => Path.Name;

    /// <summary>The removal facts as far as the scan knows them; the branch flags are settled by the action.</summary>
    public WorktreeRemovalFacts Facts(bool branchCreatedByVantage = false, bool branchMerged = false) => new(Health.Kind, Branch?.Value ?? FolderName, DirtyFiles, UnpushedCommits, SessionName, branchCreatedByVantage, branchMerged, LeftoverFileCount);
}