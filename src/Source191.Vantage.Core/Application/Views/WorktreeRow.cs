using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// One line of the worktrees table: a linked worktree or a leftover folder, with everything the removal rules need
/// except the two facts only the state store and git can add (whether Vantage created the branch, and whether it is merged).
/// Repository is the owning main checkout (for a leftover whose owner is gone, where it used to be; for an empty
/// leftover, the root). OwnerExists says whether that repository still has a .git folder, so a stale registration can be pruned.
/// A leftover's head is Unknown: git no longer lists it, so nothing names its branch.
/// </summary>
public sealed record WorktreeRow(
    FolderPath Repository,
    FolderPath Path,
    Head Head,
    WorktreeHealth Health,
    int DirtyFiles,
    int UnpushedCommits,
    LastCommit LastCommitAt,
    WorktreeSession Session,
    int LeftoverFileCount,
    bool OwnerExists,
    bool FolderExists)
{
    /// <summary>The branch's pull request, or why there is none; a leftover has no branch to look up.</summary>
    public PullRequestLookup PullRequest { get; init; } = new PullRequestLookup.Missing(PullRequestGap.NoBranch);

    public string RepositoryName => Repository.Name;

    public string FolderName => Path.Name;



    /// <summary>The removal facts as far as the scan knows them; the branch flags are settled by the action. The folder name stands in for a branch the head does not name.</summary>
    public WorktreeRemovalFacts Facts(bool branchCreatedByVantage = false, bool branchMerged = false) => new(Health.Kind, Head is Head.OnBranch on ? on.Branch.Value : FolderName, DirtyFiles, UnpushedCommits, Session, branchCreatedByVantage, branchMerged, LeftoverFileCount);
}