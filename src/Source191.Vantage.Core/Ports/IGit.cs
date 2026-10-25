namespace Source191.Vantage.Ports;

/// <summary>
/// The git commands Vantage needs. Every call names the directory it runs in; nothing depends on the current directory.
/// The convention: the commands (list, add, remove, prune, count, delete) throw <see cref="GitException"/> when git
/// fails or answers with something unreadable; the queries that return a nullable answer null instead; IsMerged
/// answers false.
/// </summary>
public interface IGit
{
    /// <summary>Throws <see cref="GitException"/> when git fails.</summary>
    Task<IReadOnlyList<GitWorktreeEntry>> ListWorktreesAsync(string repository, CancellationToken cancellationToken);

    /// <summary>Throws <see cref="GitException"/> when git fails, for example when the branch is checked out elsewhere.</summary>
    Task AddWorktreeAsync(string repository, string path, string branch, string baseRef, CancellationToken cancellationToken);

    /// <summary>Throws <see cref="GitException"/> when git fails, for example on changes without force.</summary>
    Task RemoveWorktreeAsync(string repository, string path, bool force, CancellationToken cancellationToken);

    /// <summary>How many stale registrations went. Throws <see cref="GitException"/> when git fails.</summary>
    Task<int> PruneWorktreesAsync(string repository, CancellationToken cancellationToken);

    /// <summary>Throws <see cref="GitException"/> when git fails.</summary>
    Task<int> CountChangedFilesAsync(string worktree, CancellationToken cancellationToken);

    /// <summary>Commits on no remote branch. Throws <see cref="GitException"/> when git fails or its count is not a number.</summary>
    Task<int> CountUnpushedCommitsAsync(string worktree, CancellationToken cancellationToken);

    /// <summary>Null when git fails or there are no commits.</summary>
    Task<DateTimeOffset?> LastCommitAtAsync(string worktree, CancellationToken cancellationToken);

    /// <summary>Throws <see cref="GitException"/> when git fails, for example on an unmerged branch without force.</summary>
    Task DeleteBranchAsync(string repository, string branch, bool force, CancellationToken cancellationToken);

    /// <summary>Origin's HEAD, such as origin/main; null when git fails or origin has none.</summary>
    Task<string?> DefaultRemoteBranchAsync(string repository, CancellationToken cancellationToken);

    /// <summary>Whether <paramref name="branch"/> is an ancestor of <paramref name="into"/>; false when git fails.</summary>
    Task<bool> IsMergedAsync(string repository, string branch, string into, CancellationToken cancellationToken);

    /// <summary>Null when git fails or the head is detached.</summary>
    Task<string?> CurrentBranchAsync(string directory, CancellationToken cancellationToken);

    /// <summary>The checkout's root as a Windows path; null when git fails, as it does outside a repository.</summary>
    Task<string?> RepositoryRootAsync(string directory, CancellationToken cancellationToken);

    /// <summary>The main checkout a linked worktree belongs to; null when git fails.</summary>
    Task<string?> MainCheckoutOfAsync(string directory, CancellationToken cancellationToken);

    /// <summary>The origin remote's URL as git has it; null when git fails, as it does when there is no origin.</summary>
    Task<string?> OriginUrlAsync(string repository, CancellationToken cancellationToken);

    /// <summary>Repository-relative paths, with forward slashes, of every file git ignores in the checkout. Throws <see cref="GitException"/> when git fails.</summary>
    Task<IReadOnlyList<string>> ListIgnoredFilesAsync(string repository, CancellationToken cancellationToken);
}