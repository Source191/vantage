namespace Source191.Vantage.Domain;

/// <summary>
/// What the file system and git say about one worktree folder, before any judgement is made about it.
/// </summary>
/// <param name="RegisteredWithGit">The folder appears in the repository's worktree listing.</param>
/// <param name="FolderExists">The folder exists on disc.</param>
/// <param name="Lock">Whether git has the worktree locked, with its reason.</param>
/// <param name="UnderClaudeWorktrees">The folder sits under a repository's .claude\worktrees directory.</param>
/// <param name="GitFile">For an unregistered folder, what its .git file points at.</param>
/// <param name="EmptyLeftoverFolder">The folder is an empty leftover such as a .worktrees folder.</param>
/// <param name="Prunable">Git lists the worktree as prunable: the .git file its registration names is missing.</param>
public sealed record WorktreeObservation(
    bool RegisteredWithGit,
    bool FolderExists,
    WorktreeLock Lock,
    bool UnderClaudeWorktrees,
    GitPointer GitFile,
    bool EmptyLeftoverFolder,
    bool Prunable);