using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Screens;

namespace Source191.Vantage.Actions;

public sealed record WorktreeRemovalOutcome(string Message, BranchToConfirm? BranchToConfirm);

/// <summary>
/// Removal, prune and shell actions for the worktrees view. The decision comes from the domain rules; this class
/// adds the two facts only it can settle (did Vantage create the branch, is it merged) and then carries the decision out.
/// </summary>
public sealed class WorktreeActions(IGit git, StateStore store, IWindowsTerminal terminal)
{
    private static readonly TabColour ShellTabColour = TabPalette.Default[^1];

    public async Task<WorktreeRemovalOutcome> RemoveAsync(WorktreeRow row, bool forced, CancellationToken cancellationToken)
    {
        CreatedWorktree? created = (await store.ReadCreatedWorktreesAsync())
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

    public async Task<string> PruneAsync(string repository, CancellationToken cancellationToken)
    {
        int pruned = await git.PruneWorktreesAsync(repository, cancellationToken);

        return $"Pruned {Count(pruned, "stale registration")} from {new RepositoryPath(repository).Name}.";
    }

    public async Task<string> OpenShellAsync(WorktreeRow row, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(new TabLaunch(row.FolderName, ShellTabColour, row.Path, "Clear-Host"), cancellationToken);

        return $"Opened a shell in {row.FolderName}.";
    }

    public Task DeleteBranchAsync(BranchToConfirm branch, CancellationToken cancellationToken)
    {
        return git.DeleteBranchAsync(branch.Repository, branch.Branch, force: true, cancellationToken);
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
        await store.ForgetCreatedWorktreeAsync(row.Path);
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
        FolderDeletion.DeleteTree(row.Path);
        if (!decision.PruneRegistration || !row.OwnerExists)
        {
            return new WorktreeRemovalOutcome($"Deleted the folder {row.FolderName}.", BranchToConfirm: null);
        }

        int pruned = await git.PruneWorktreesAsync(row.Repository, cancellationToken);

        return new WorktreeRemovalOutcome(
            $"Deleted the folder {row.FolderName} and pruned {Count(pruned, "stale registration")} from {row.RepositoryName}.",
            BranchToConfirm: null);
    }

    private static string Count(int number, string noun)
    {
        return number == 1 ? $"1 {noun}" : $"{number} {noun}s";
    }
}