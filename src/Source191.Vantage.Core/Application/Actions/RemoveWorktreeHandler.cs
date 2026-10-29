using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Removes a worktree under the removal rules. The decision comes from the domain; this class adds the two facts only
/// it can settle (did Vantage create the branch, is it merged) and hands the decision to <see cref="WorktreeRemover"/>.
/// Not a dispatcher handler: the screen may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class RemoveWorktreeHandler(IGit git, ICreatedWorktrees createdWorktrees, IFolders folders)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly WorktreeRemover remover = new(git, createdWorktrees, folders);

    public async Task<RemoveWorktreeOutcome> RemoveAsync(WorktreeRow row, bool forced, CancellationToken cancellationToken)
    {
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(candidate => candidate.Path == row.Path);
        // Merge state needs a branch and a repository that still exists.
        bool merged = created is not null && row is { Branch: BranchName branch, OwnerExists: true } && await inspector.IsMergedAsync(row.Repository, branch, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(created is not null, merged), forced);

        return await remover.CarryOutAsync(row, decision, createdByVantage: created is not null, cancellationToken);
    }
}
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
}