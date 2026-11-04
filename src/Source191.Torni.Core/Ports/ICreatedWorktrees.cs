using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>The worktrees and branches Torni created, which decide the branch-deletion rules.</summary>
public interface ICreatedWorktrees
{
    Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync();

    /// <summary>Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> when the file system refuses the write.</summary>
    Task RecordCreatedWorktreeAsync(CreatedWorktree worktree);

    Task ForgetCreatedWorktreeAsync(FolderPath path);
}