using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Carries out a removal decision for a worktree, the one way both views remove one: refuse, delete a leftover folder
/// (pruning its registration when the decision says so), or remove it through git and then deal with its branch.
/// </summary>
public sealed class WorktreeRemover(IGit git, ICreatedWorktrees createdWorktrees, IFolders folders)
{
    public async Task<RemoveWorktreeOutcome> CarryOutAsync(WorktreeRow row, RemovalDecision decision, bool createdByVantage, CancellationToken cancellationToken)
    {
        return decision.Outcome switch
        {
            RemovalOutcome.Refused => new RemoveWorktreeOutcome.Refused(decision.Reason),
            RemovalOutcome.DeleteFolder => await DeleteFolderAsync(row, decision, cancellationToken),
            RemovalOutcome.RemoveWorktree => await RemoveThroughGitAsync(row, decision, createdByVantage, cancellationToken),
        };
    }

    private async Task<RemoveWorktreeOutcome> RemoveThroughGitAsync(WorktreeRow row, RemovalDecision decision, bool createdByVantage, CancellationToken cancellationToken)
    {
        await git.RemoveWorktreeAsync(row.Repository.Value, row.Path.Value, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(row.Path);
        // A worktree on a detached head has no branch; the folder name stands in, as the removal rules assume.
        BranchName? branch = row.Branch;
        if (branch is null && BranchName.TryParse(row.FolderName, out BranchName? folderBranch))
        {
            branch = folderBranch;
        }

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