using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>What git says about a worktree's work: changed files, commits not pushed, and when it last committed.</summary>
public sealed record WorktreeCounts(int DirtyFiles, int UnpushedCommits, LastCommit LastCommitAt);

/// <summary>
/// The one place git-backed worktree facts are gathered: the counts the scan and the removal rules need, the health
/// git's own listing gives a worktree, and whether its branch is merged into the remote default. Leftover-file counts
/// are a file-system fact and stay with the scan.
/// </summary>
public sealed class WorktreeInspector(IGit git)
{
    public async Task<WorktreeCounts> CountAsync(FolderPath worktree, CancellationToken cancellationToken) => new(
        await git.CountChangedFilesAsync(worktree.Value, cancellationToken),
        await git.CountUnpushedCommitsAsync(worktree.Value, cancellationToken),
        await git.LastCommitAtAsync(worktree.Value, cancellationToken) is DateTimeOffset at ? new LastCommit.Made(at) : new LastCommit.Unknown());

    /// <summary>
    /// Health as git's listing and the disc give it, the caller having read whether the folder exists: listed and fine
    /// is ok or locked with git's reason; listed and prunable is broken, its folder gone or only its .git file; not
    /// listed is broken with its folder gone, or, while the folder exists (git may list it under another spelling, such
    /// as the real folder behind a junction), not registered, so its files are counted before anything is deleted.
    /// </summary>
    public async Task<WorktreeHealth> HealthAsync(FolderPath repository, FolderPath worktree, bool folderExists, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(repository.Value, cancellationToken);
        GitWorktreeEntry? entry = entries.FirstOrDefault(candidate => FolderPath.TryParse(candidate.Path, out FolderPath? path) && path.Equals(worktree));

        return entry is null
            ? WorktreeHealth.From(new WorktreeObservation(RegisteredWithGit: !folderExists, folderExists, new WorktreeLock.Unlocked(), false, new GitPointer.NoGitFile(), false, Prunable: false))
            : WorktreeHealth.From(new WorktreeObservation(true, folderExists, entry.Lock, false, new GitPointer.NoGitFile(), false, entry.IsPrunable));
    }

    /// <summary>Merged into the remote default branch, or into HEAD when the repository has none.</summary>
    public async Task<bool> IsMergedAsync(FolderPath repository, BranchName branch, CancellationToken cancellationToken) => (await MergeStateAsync(repository, branch, cancellationToken)).Merged;

    /// <summary>What the branch is checked against (origin's default branch, or HEAD when the repository has none) and whether it is merged into it.</summary>
    public async Task<Detail.MergeState> MergeStateAsync(FolderPath repository, BranchName branch, CancellationToken cancellationToken)
    {
        string into = await git.DefaultRemoteBranchAsync(repository.Value, cancellationToken) ?? "HEAD";

        return new Detail.MergeState(into, await git.IsMergedAsync(repository.Value, branch.Value, into, cancellationToken));
    }
}