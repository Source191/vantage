using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>One block of git's machine-readable worktree listing, as <see cref="IGit.ListWorktreesAsync"/> returns it: Commit is the hash on its HEAD line.</summary>
public sealed record GitWorktreeEntry(
    string Path,
    string Commit,
    Head Head,
    bool IsMain,
    string? LockReason,
    string? PrunableReason);