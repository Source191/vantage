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

    /// <summary>Health as git's listing gives it: broken when git no longer lists the worktree or calls it prunable, locked with git's reason.</summary>
    public async Task<WorktreeHealth> HealthAsync(FolderPath repository, FolderPath worktree, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(repository.Value, cancellationToken);
        GitWorktreeEntry? entry = entries.FirstOrDefault(candidate => FolderPath.TryParse(candidate.Path, out FolderPath? path) && path.Equals(worktree));

        return entry is null
            ? WorktreeHealth.From(new WorktreeObservation(true, false, new WorktreeLock.Unlocked(), false, new GitPointer.NoGitFile(), false, Prunable: false))
            : WorktreeHealth.From(new WorktreeObservation(true, !entry.IsPrunable, entry.Lock, false, new GitPointer.NoGitFile(), false, Prunable: false));
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