using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Deletes a session and, when it lived in a worktree Vantage created, removes that worktree under the removal rules.
/// Not a dispatcher handler: the screen confirms first and may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class DeleteSessionHandler(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees)
{
    public async Task<DeleteSessionOutcome> DeleteAsync(SessionRow row, bool forced, CancellationToken cancellationToken)
    {
        await claude.RemoveAsync(row.Session.JobId!, cancellationToken);
        string name = row.Session.Name;

        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(worktree => string.Equals(worktree.Path, row.Session.WorkingDirectory, StringComparison.OrdinalIgnoreCase));
        if (created is null)
        {
            return new DeleteSessionOutcome.Deleted(name);
        }

        RemovalDecision decision = RemovalDecision.For(await FactsAsync(created, cancellationToken), forced);
        if (decision.Outcome != RemovalOutcome.RemoveWorktree)
        {
            return new DeleteSessionOutcome.KeptWorktree(name, decision.Reason);
        }

        await git.RemoveWorktreeAsync(created.Repository, created.Path, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(created.Path);
        if (decision.Branch == BranchAction.Delete)
        {
            await git.DeleteBranchAsync(created.Repository, created.Branch, force: false, cancellationToken);

            return new DeleteSessionOutcome.RemovedWorktreeAndBranch(name, created.Branch);
        }

        return new DeleteSessionOutcome.BranchNotMerged(name, new BranchToConfirm(created.Repository, created.Branch));
    }

    private async Task<WorktreeRemovalFacts> FactsAsync(CreatedWorktree created, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(created.Repository, cancellationToken);
        GitWorktreeEntry? entry = entries.FirstOrDefault(candidate => string.Equals(candidate.Path, created.Path, StringComparison.OrdinalIgnoreCase));
        // Git's listing is the source of truth here: a prunable entry means the folder has gone.
        WorktreeHealthKind health = entry is null
            ? WorktreeHealthKind.Broken
            : WorktreeHealth.From(new WorktreeObservation(true, entry.PrunableReason is null, entry.LockReason, false, null, false)).Kind;
        string into = await git.DefaultRemoteBranchAsync(created.Repository, cancellationToken) ?? "HEAD";

        return new WorktreeRemovalFacts(
            health,
            created.Branch,
            DirtyFileCount: await git.CountChangedFilesAsync(created.Path, cancellationToken),
            UnpushedCommitCount: await git.CountUnpushedCommitsAsync(created.Path, cancellationToken),
            LiveSessionName: null,
            BranchCreatedByVantage: true,
            BranchMerged: await git.IsMergedAsync(created.Repository, created.Branch, into, cancellationToken),
            LeftoverFileCount: 0);
    }
}

/// <summary>What deleting a session did, and in the last case the unmerged branch the user is asked about.</summary>
public abstract record DeleteSessionOutcome : Outcome
{
    public sealed record Deleted(string Name) : DeleteSessionOutcome;

    public sealed record KeptWorktree(string Name, string Reason) : DeleteSessionOutcome;

    public sealed record RemovedWorktreeAndBranch(string Name, string Branch) : DeleteSessionOutcome;

    public sealed record BranchNotMerged(string Name, BranchToConfirm Branch) : DeleteSessionOutcome;
}