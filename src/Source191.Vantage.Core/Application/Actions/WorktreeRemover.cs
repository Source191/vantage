using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

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
        try
        {
            await git.RemoveWorktreeAsync(row.Repository.Value, row.Path.Value, decision.Forced, cancellationToken);
        }
        catch (GitException exception) when (exception.InUse)
        {
            // Git deleted what it could; the registration stays, so a later remove sees a leftover and finishes.
            return new RemoveWorktreeOutcome.InUse(row.FolderName);
        }

        await createdWorktrees.ForgetCreatedWorktreeAsync(row.Path);
        return (decision.Branch, BranchToDealWith(row)) switch
        {
            (BranchAction.Delete, Head.OnBranch merged) => await DeleteBranchAsync(row, merged.Branch, cancellationToken),
            (BranchAction.Ask, Head.OnBranch kept) => new RemoveWorktreeOutcome.BranchKept(row.FolderName, new BranchToConfirm(row.Repository, kept.Branch), createdByVantage),
            (BranchAction.None, _) or (_, Head.Detached or Head.Unknown) => new RemoveWorktreeOutcome.Removed(row.FolderName),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>The row's head when it is on a branch; otherwise the folder name stands in when it is a branch name, as the removal rules assume.</summary>
    private static Head BranchToDealWith(WorktreeRow row) => row.Head switch
    {
        Head.OnBranch => row.Head,
        Head.Detached or Head.Unknown => BranchName.TryParse(row.FolderName, out BranchName? folderBranch) ? new Head.OnBranch(folderBranch) : row.Head,
        _ => throw new UnreachableException(),
    };

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