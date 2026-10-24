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
    public async Task<WorktreeRemovalOutcome> RemoveAsync(WorktreeRow row, bool forced, CancellationToken cancellationToken)
    {
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(candidate => string.Equals(candidate.Path, row.Path, StringComparison.OrdinalIgnoreCase));
        bool merged = created is not null && await IsMergedAsync(row, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(created is not null, merged), forced);

        return decision.Outcome switch
        {
            RemovalOutcome.Refused => new WorktreeRemovalOutcome($"Not removed: {decision.Reason}.", BranchToConfirm: null),
            RemovalOutcome.DeleteFolder => await DeleteFolderAsync(row, decision, cancellationToken),
            _ => await RemoveThroughGitAsync(row, decision, cancellationToken),
        };
    }

    private async Task<bool> IsMergedAsync(WorktreeRow row, CancellationToken cancellationToken)
    {
        if (row.Branch is null || !row.OwnerExists)
        {
            return false;
        }

        string into = await git.DefaultRemoteBranchAsync(row.Repository, cancellationToken) ?? "HEAD";

        return await git.IsMergedAsync(row.Repository, row.Branch, into, cancellationToken);
    }

    private async Task<WorktreeRemovalOutcome> RemoveThroughGitAsync(WorktreeRow row, RemovalDecision decision, CancellationToken cancellationToken)
    {
        await git.RemoveWorktreeAsync(row.Repository, row.Path, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(row.Path);
        string branch = row.Branch ?? row.FolderName;

        switch (decision.Branch)
        {
            case BranchAction.Delete:
                await git.DeleteBranchAsync(row.Repository, branch, force: false, cancellationToken);

                return new WorktreeRemovalOutcome($"Removed {row.FolderName} and deleted its merged branch {branch}.", BranchToConfirm: null);
            case BranchAction.Ask:
                return new WorktreeRemovalOutcome(
                    $"Removed {row.FolderName}; branch {branch} was not created by Vantage, so it is kept.",
                    new BranchToConfirm(row.Repository, branch));
            default:
                return new WorktreeRemovalOutcome($"Removed {row.FolderName}.", BranchToConfirm: null);
        }
    }

    private async Task<WorktreeRemovalOutcome> DeleteFolderAsync(WorktreeRow row, RemovalDecision decision, CancellationToken cancellationToken)
    {
        folders.DeleteTree(row.Path);
        if (!decision.PruneRegistration || !row.OwnerExists)
        {
            return new WorktreeRemovalOutcome($"Deleted the folder {row.FolderName}.", BranchToConfirm: null);
        }

        int pruned = await git.PruneWorktreesAsync(row.Repository, cancellationToken);

        return new WorktreeRemovalOutcome(
            $"Deleted the folder {row.FolderName} and pruned {Registrations.Count(pruned)} from {row.RepositoryName}.",
            BranchToConfirm: null);
    }
}