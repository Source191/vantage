using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The worktrees and branches Vantage created, which decide the branch-deletion rules.</summary>
public interface ICreatedWorktrees
{
    Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync();

    /// <summary>Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> when the file system refuses the write.</summary>
    Task RecordCreatedWorktreeAsync(CreatedWorktree worktree);

    Task ForgetCreatedWorktreeAsync(FolderPath path);
}