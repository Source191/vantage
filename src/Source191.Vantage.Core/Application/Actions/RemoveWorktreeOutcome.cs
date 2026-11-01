using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What removing a worktree did, and in one case the branch the user is asked about.</summary>
public abstract record RemoveWorktreeOutcome : Outcome
{
    public sealed record Refused(RemovalReason Reason) : RemoveWorktreeOutcome;

    public sealed record Removed(string FolderName) : RemoveWorktreeOutcome;

    public sealed record RemovedAndDeletedBranch(string FolderName, BranchName Branch) : RemoveWorktreeOutcome;

    /// <summary>The branch was kept: Vantage did not create it, or created it and it is not merged. The user is asked.</summary>
    public sealed record BranchKept(string FolderName, BranchToConfirm Branch, bool CreatedByVantage) : RemoveWorktreeOutcome;

    public sealed record DeletedFolder(string FolderName) : RemoveWorktreeOutcome;

    public sealed record DeletedFolderAndPruned(string FolderName, int Pruned, string RepositoryName) : RemoveWorktreeOutcome;

    /// <summary>Git could not delete the folder because a tab or program still has it open; what it could delete is gone.</summary>
    public sealed record InUse(string FolderName) : RemoveWorktreeOutcome;
}