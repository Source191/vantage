using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Deletes a session and, when it lived in a worktree Vantage created, removes that worktree under the removal rules.
/// Not a dispatcher handler: the screen confirms first and may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class DeleteSessionHandler(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees)
{
    private readonly WorktreeInspector inspector = new(git);

    public async Task<DeleteSessionOutcome> DeleteAsync(BackgroundSession session, bool forced, CancellationToken cancellationToken)
    {
        await claude.RemoveAsync(session.JobId.Value, cancellationToken);
        string name = session.Name;

        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(worktree => worktree.Path == session.WorkingDirectory);
        if (created is null)
        {
            return new DeleteSessionOutcome.Deleted(name);
        }

        RemovalDecision decision = RemovalDecision.For(await FactsAsync(created, cancellationToken), forced);
        if (decision.Outcome != RemovalOutcome.RemoveWorktree)
        {
            return new DeleteSessionOutcome.KeptWorktree(name, decision.Reason);
        }

        await git.RemoveWorktreeAsync(created.Repository.Value, created.Path.Value, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(created.Path);
        if (decision.Branch == BranchAction.Delete)
        {
            await git.DeleteBranchAsync(created.Repository.Value, created.Branch.Value, force: false, cancellationToken);

            return new DeleteSessionOutcome.RemovedWorktreeAndBranch(name, created.Branch);
        }

        return new DeleteSessionOutcome.BranchNotMerged(name, new BranchToConfirm(created.Repository, created.Branch));
    }

    private async Task<WorktreeRemovalFacts> FactsAsync(CreatedWorktree created, CancellationToken cancellationToken)
    {
        // Git's listing is the source of truth here: a prunable entry means the folder has gone.
        WorktreeHealth health = await inspector.HealthAsync(created.Repository, created.Path, cancellationToken);
        WorktreeCounts counts = await inspector.CountAsync(created.Path, cancellationToken);

        return new WorktreeRemovalFacts(
            health.Kind,
            created.Branch.Value,
            counts.DirtyFiles,
            counts.UnpushedCommits,
            LiveSessionName: null,
            BranchCreatedByVantage: true,
            BranchMerged: await inspector.IsMergedAsync(created.Repository, created.Branch, cancellationToken),
            LeftoverFileCount: 0);
    }
}

/// <summary>What deleting a session did, and in the last case the unmerged branch the user is asked about.</summary>
public abstract record DeleteSessionOutcome : Outcome
{
    public sealed record Deleted(string Name) : DeleteSessionOutcome;

    public sealed record KeptWorktree(string Name, string Reason) : DeleteSessionOutcome;

    public sealed record RemovedWorktreeAndBranch(string Name, BranchName Branch) : DeleteSessionOutcome;

    public sealed record BranchNotMerged(string Name, BranchToConfirm Branch) : DeleteSessionOutcome;
}