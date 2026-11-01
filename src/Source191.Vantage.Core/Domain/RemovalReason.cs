namespace Source191.Vantage.Domain;

/// <summary>Why the removal rules said no, or what they did instead, as a fact; <c>Wording</c> turns each case into the words shown.</summary>
public abstract record RemovalReason
{
    /// <summary>A plain removal: nothing to explain.</summary>
    public sealed record None : RemovalReason;

    public sealed record LiveSession(string Name) : RemovalReason;

    public sealed record Locked : RemovalReason;

    /// <summary>Files git no longer tracks; AtLeast is set when the count stopped at the inspection limit.</summary>
    public sealed record LeftoverFilesWouldBeLost(int Count, bool AtLeast) : RemovalReason;

    public sealed record ChangesWouldBeLost(int Count) : RemovalReason;

    public sealed record UnpushedCommits(string Branch, int Count) : RemovalReason;

    /// <summary>Only the folder is deleted, because git no longer has the worktree.</summary>
    public sealed record LeftoverFolder : RemovalReason;

    /// <summary>A live worktree of a repository outside the roots: not a leftover, so never deleted as one.</summary>
    public sealed record RepositoryOutsideRoots : RemovalReason;

    /// <summary>A leftover folder that is not under a repository root or the worktree root: never deleted, not even when forced.</summary>
    public sealed record OutsideTheRoots(FolderPath Folder) : RemovalReason;
}