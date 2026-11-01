namespace Source191.Vantage.Ports;

/// <summary>Copies the gitignored files a repository's .worktreeinclude names into a new worktree.</summary>
public interface IWorktreeIncludeCopier
{
    Task<IReadOnlyList<string>> CopyAsync(string repository, string worktree, CancellationToken cancellationToken);
}