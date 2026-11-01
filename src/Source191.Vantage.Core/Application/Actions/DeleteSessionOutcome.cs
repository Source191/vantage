using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What deleting a session did, and in one case the unmerged branch the user is asked about. TabClosed when a Vantage tab showing it was closed.</summary>
public abstract record DeleteSessionOutcome : Outcome
{
    public bool TabClosed { get; init; }

    public sealed record Deleted(string Name) : DeleteSessionOutcome;

    public sealed record KeptWorktree(string Name, RemovalReason Reason) : DeleteSessionOutcome;

    public sealed record RemovedWorktreeAndBranch(string Name, BranchName Branch) : DeleteSessionOutcome;

    public sealed record BranchNotMerged(string Name, BranchToConfirm Branch) : DeleteSessionOutcome;

    /// <summary>The worktree went another way than through git with its branch: its leftover folder deleted, perhaps pruned.</summary>
    public sealed record RemovedWorktree(string Name, RemoveWorktreeOutcome Worktree) : DeleteSessionOutcome;

    /// <summary>The session is deleted; its worktree folder could not be, because a tab or program still has it open.</summary>
    public sealed record WorktreeInUse(string Name, string FolderName) : DeleteSessionOutcome;
}