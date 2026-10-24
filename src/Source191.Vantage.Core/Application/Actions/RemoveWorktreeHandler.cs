using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Removes a worktree under the removal rules. The decision comes from the domain; this class adds the two facts only
/// it can settle (did Vantage create the branch, is it merged) and then carries the decision out. Not a dispatcher
/// handler: the screen may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class RemoveWorktreeHandler(IGit git, ICreatedWorktrees createdWorktrees, IFolders folders)
{
    public async Task<RemoveWorktreeOutcome> RemoveAsync(WorktreeRow row, bool forced, CancellationToken cancellationToken)
    {
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(candidate => candidate.Path == row.Path);
        bool merged = created is not null && await IsMergedAsync(row, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(created is not null, merged), forced);

        return decision.Outcome switch
        {
            RemovalOutcome.Refused => new RemoveWorktreeOutcome.Refused(decision.Reason),
            RemovalOutcome.DeleteFolder => await DeleteFolderAsync(row, decision, cancellationToken),
            RemovalOutcome.RemoveWorktree => await RemoveThroughGitAsync(row, decision, createdByVantage: created is not null, cancellationToken),
        };
    }

    private async Task<bool> IsMergedAsync(WorktreeRow row, CancellationToken cancellationToken)
    {
        if (row.Branch is null || !row.OwnerExists)
        {
            return false;
        }

        string into = await git.DefaultRemoteBranchAsync(row.Repository.Value, cancellationToken) ?? "HEAD";

        return await git.IsMergedAsync(row.Repository.Value, row.Branch.Value, into, cancellationToken);
    }

    private async Task<RemoveWorktreeOutcome> RemoveThroughGitAsync(WorktreeRow row, RemovalDecision decision, bool createdByVantage, CancellationToken cancellationToken)
    {
        await git.RemoveWorktreeAsync(row.Repository.Value, row.Path.Value, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(row.Path);
        // A worktree on a detached head has no branch; the folder name stands in, as the removal rules assume.
        BranchName? branch = row.Branch ?? BranchName.TryParse(row.FolderName);

        return (decision.Branch, branch) switch
        {
            (BranchAction.Delete, BranchName merged) => await DeleteBranchAsync(row, merged, cancellationToken),
            (BranchAction.Ask, BranchName kept) => new RemoveWorktreeOutcome.BranchKept(row.FolderName, new BranchToConfirm(row.Repository, kept), createdByVantage),
            (BranchAction.None, _) or (_, null) => new RemoveWorktreeOutcome.Removed(row.FolderName),
        };
    }

    private async Task<RemoveWorktreeOutcome> DeleteBranchAsync(WorktreeRow row, BranchName branch, CancellationToken cancellationToken)
    {
        await git.DeleteBranchAsync(row.Repository.Value, branch.Value, force: false, cancellationToken);

        return new RemoveWorktreeOutcome.RemovedAndDeletedBranch(row.FolderName, branch);
    }

    private async Task<RemoveWorktreeOutcome> DeleteFolderAsync(WorktreeRow row, RemovalDecision decision, CancellationToken cancellationToken)
    {
        folders.DeleteTree(row.Path.Value);
        if (!decision.PruneRegistration || !row.OwnerExists)
        {
            return new RemoveWorktreeOutcome.DeletedFolder(row.FolderName);
        }

        int pruned = await git.PruneWorktreesAsync(row.Repository.Value, cancellationToken);

        return new RemoveWorktreeOutcome.DeletedFolderAndPruned(row.FolderName, pruned, row.RepositoryName);
    }
}

/// <summary>What removing a worktree did, and in one case the branch the user is asked about.</summary>
public abstract record RemoveWorktreeOutcome : Outcome
{
    public sealed record Refused(string Reason) : RemoveWorktreeOutcome;

    public sealed record Removed(string FolderName) : RemoveWorktreeOutcome;

    public sealed record RemovedAndDeletedBranch(string FolderName, BranchName Branch) : RemoveWorktreeOutcome;

    /// <summary>The branch was kept: Vantage did not create it, or created it and it is not merged. The user is asked.</summary>
    public sealed record BranchKept(string FolderName, BranchToConfirm Branch, bool CreatedByVantage) : RemoveWorktreeOutcome;

    public sealed record DeletedFolder(string FolderName) : RemoveWorktreeOutcome;

    public sealed record DeletedFolderAndPruned(string FolderName, int Pruned, string RepositoryName) : RemoveWorktreeOutcome;
}