using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Carries out a removal decision for a worktree, the one way both views remove one: refuse, delete a leftover folder
/// (pruning its registration when the decision says so), or remove it through git and then deal with its branch. The
/// last guard before a folder is deleted from disc is that it lies inside a repository root or the worktree root, read
/// from the settings as they are now, whoever asked and whatever the decision said.
/// </summary>
public sealed class WorktreeRemover(IGit git, ICreatedWorktrees createdWorktrees, IFolderDeleter deleter, CurrentSettings settings)
{
    public async Task<RemoveWorktreeOutcome> CarryOutAsync(WorktreeRow row, RemovalDecision decision, bool createdHere, CancellationToken cancellationToken)
    {
        return decision switch
        {
            RemovalDecision.Refused refused => new RemoveWorktreeOutcome.Refused(refused.Reason),
            RemovalDecision.DeleteFolder deleteFolder => await DeleteFolderAsync(row, deleteFolder, cancellationToken),
            RemovalDecision.RemoveWorktree removeWorktree => await RemoveThroughGitAsync(row, removeWorktree, createdHere, cancellationToken),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<RemoveWorktreeOutcome> RemoveThroughGitAsync(WorktreeRow row, RemovalDecision.RemoveWorktree decision, bool createdHere, CancellationToken cancellationToken)
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
        return (decision.Branch, row.Head) switch
        {
            (BranchAction.Delete, Head.OnBranch merged) => await DeleteBranchAsync(row, merged.Branch, cancellationToken),
            (BranchAction.Ask, Head.OnBranch kept) => new RemoveWorktreeOutcome.BranchKept(row.FolderName, new BranchToConfirm(row.Repository, kept.Branch), createdHere),
            (_, Head.Detached or Head.Unknown) => new RemoveWorktreeOutcome.Removed(row.FolderName),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<RemoveWorktreeOutcome> DeleteBranchAsync(WorktreeRow row, BranchName branch, CancellationToken cancellationToken)
    {
        await git.DeleteBranchAsync(row.Repository.Value, branch.Value, force: false, cancellationToken);

        return new RemoveWorktreeOutcome.RemovedAndDeletedBranch(row.FolderName, branch);
    }

    private async Task<RemoveWorktreeOutcome> DeleteFolderAsync(WorktreeRow row, RemovalDecision.DeleteFolder decision, CancellationToken cancellationToken)
    {
        if (!InsideTheRoots(row.Path))
        {
            return new RemoveWorktreeOutcome.Refused(new RemovalReason.OutsideTheRoots(row.Path));
        }

        try
        {
            deleter.DeleteTree(row.Path.Value);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return new RemoveWorktreeOutcome.InUse(row.FolderName);
        }

        if (!decision.Prune || !row.OwnerExists)
        {
            return new RemoveWorktreeOutcome.DeletedFolder(row.FolderName);
        }

        int pruned = await git.PruneWorktreesAsync(row.Repository.Value, cancellationToken);

        return new RemoveWorktreeOutcome.DeletedFolderAndPruned(row.FolderName, pruned, row.RepositoryName);
    }

    /// <summary>Strictly inside a repository root or the worktree root: a root itself is never deleted as a leftover.</summary>
    private bool InsideTheRoots(FolderPath folder)
    {
        Settings now = settings.Value;

        return now.RepositoryRoots.Append(now.WorktreeRoot).Any(root => folder.IsUnder(root) && !folder.Equals(root));
    }
}