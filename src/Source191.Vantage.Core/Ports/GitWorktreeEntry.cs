using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>One block of git's machine-readable worktree listing, as <see cref="IGit.ListWorktreesAsync"/> returns it.</summary>
public sealed record GitWorktreeEntry(
    string Path,
    string Head,
    BranchName? Branch,
    bool IsMain,
    string? LockReason,
    string? PrunableReason);