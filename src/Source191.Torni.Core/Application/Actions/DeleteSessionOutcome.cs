using Source191.Torni.Application.Effects;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>What deleting a session did, and in one case the unmerged branch the user is asked about. TabClosed when a Torni tab showing it was closed.</summary>
public abstract record DeleteSessionOutcome(bool TabClosed) : Outcome
{
    public sealed record Deleted(ListedName Name, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    /// <summary>Claude refused to remove the job: the session is still listed, its tab is open and its worktree untouched.</summary>
    public sealed record NotDeleted(ListedName Name, string Failure, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    public sealed record KeptWorktree(ListedName Name, RemovalReason Reason, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    public sealed record RemovedWorktreeAndBranch(ListedName Name, BranchName Branch, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    public sealed record BranchNotMerged(ListedName Name, BranchToConfirm Branch, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    /// <summary>The worktree was a leftover: its folder deleted, and its registration pruned when stale.</summary>
    public sealed record RemovedWorktree(ListedName Name, RemoveWorktreeOutcome Worktree, bool TabClosed) : DeleteSessionOutcome(TabClosed);

    /// <summary>The session is deleted; its worktree folder could not be, because a tab or program still has it open.</summary>
    public sealed record WorktreeInUse(ListedName Name, string FolderName, bool TabClosed) : DeleteSessionOutcome(TabClosed);
}