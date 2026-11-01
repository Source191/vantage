using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The worktrees and branches Vantage created, which decide the branch-deletion rules.</summary>
public interface ICreatedWorktrees
{
    Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync();

    Task RecordCreatedWorktreeAsync(CreatedWorktree worktree);

    Task ForgetCreatedWorktreeAsync(FolderPath path);
}