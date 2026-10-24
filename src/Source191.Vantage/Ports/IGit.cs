namespace Source191.Vantage.Ports;

/// <summary>
/// The git commands Vantage needs. Every call names the directory it runs in; nothing depends on the current directory.
/// </summary>
public interface IGit
{
    Task<IReadOnlyList<GitWorktreeEntry>> ListWorktreesAsync(string repository, CancellationToken cancellationToken);

    Task AddWorktreeAsync(string repository, string path, string branch, string baseRef, CancellationToken cancellationToken);

    Task RemoveWorktreeAsync(string repository, string path, bool force, CancellationToken cancellationToken);

    Task<int> PruneWorktreesAsync(string repository, CancellationToken cancellationToken);

    Task<int> CountChangedFilesAsync(string worktree, CancellationToken cancellationToken);

    Task<int> CountUnpushedCommitsAsync(string worktree, CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastCommitAtAsync(string worktree, CancellationToken cancellationToken);

    Task DeleteBranchAsync(string repository, string branch, bool force, CancellationToken cancellationToken);

    Task<string?> DefaultRemoteBranchAsync(string repository, CancellationToken cancellationToken);

    Task<bool> IsMergedAsync(string repository, string branch, string into, CancellationToken cancellationToken);

    Task<string?> CurrentBranchAsync(string directory, CancellationToken cancellationToken);

    Task<string?> RepositoryRootAsync(string directory, CancellationToken cancellationToken);

    Task<string?> MainCheckoutOfAsync(string directory, CancellationToken cancellationToken);

    /// <summary>Repository-relative paths, with forward slashes, of every file git ignores in the checkout.</summary>
    Task<IReadOnlyList<string>> ListIgnoredFilesAsync(string repository, CancellationToken cancellationToken);
}