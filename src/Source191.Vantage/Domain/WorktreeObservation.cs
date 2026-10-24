namespace Source191.Vantage.Domain;

/// <summary>
/// What the file system and git say about one worktree folder, before any judgement is made about it.
/// </summary>
/// <param name="RegisteredWithGit">The folder appears in the repository's worktree listing.</param>
/// <param name="FolderExists">The folder exists on disc.</param>
/// <param name="LockReason">Git's lock reason, an empty string for a lock with no reason, or null when unlocked.</param>
/// <param name="UnderClaudeWorktrees">The folder sits under a repository's .claude\worktrees directory.</param>
/// <param name="GitFileTargetExists">For an unregistered folder with a .git file, whether the registration it points at still exists; null when there is no .git file.</param>
/// <param name="EmptyLeftoverFolder">The folder is an empty leftover such as a .worktrees folder.</param>
public sealed record WorktreeObservation(
    bool RegisteredWithGit,
    bool FolderExists,
    string? LockReason,
    bool UnderClaudeWorktrees,
    bool? GitFileTargetExists,
    bool EmptyLeftoverFolder);