namespace Source191.Vantage.Domain;

/// <summary>What is wrong with a worktree, as a fact; <c>Wording</c> turns each case into the words shown.</summary>
public abstract record HealthReason
{
    /// <summary>Nothing to explain.</summary>
    public sealed record None : HealthReason;

    /// <summary>Git still registers the worktree, but its folder is gone.</summary>
    public sealed record FolderGone : HealthReason;

    /// <summary>Git still registers the worktree and its folder exists, but the folder's .git file is gone.</summary>
    public sealed record GitFileGone : HealthReason;

    /// <summary>Git's own words for the lock, kept as it said them; they may be empty.</summary>
    public sealed record Locked(string LockReason) : HealthReason;

    /// <summary>The folder's .git file points at a registration that no longer exists.</summary>
    public sealed record RegistrationGone : HealthReason;

    /// <summary>A folder under .claude\worktrees that git no longer lists.</summary>
    public sealed record UnderClaudeWorktrees : HealthReason;

    /// <summary>A leftover folder with nothing in it.</summary>
    public sealed record EmptyLeftover : HealthReason;

    /// <summary>A folder git does not register as a worktree.</summary>
    public sealed record NotRegistered : HealthReason;

    /// <summary>A live worktree whose repository was not scanned because it is not under the roots.</summary>
    public sealed record RepositoryOutsideRoots(FolderPath Repository) : HealthReason;
}