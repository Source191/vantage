using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Launch;

public enum LaunchOutcomeKind
{
    Started,
    WorkspaceNotTrusted,
    Failed,
}

public sealed record LaunchOutcome(LaunchOutcomeKind Kind, string? JobId, string Message);

/// <summary>
/// Carries out a launch plan: creates the sibling worktree when one is wanted, copies the include files,
/// starts the named background session, and records what Vantage needs to remember about it.
/// </summary>
public sealed class LaunchService(IClaudeCli claude, IGit git, StateStore store, WorktreeIncludeCopier copier, TimeProvider clock)
{
    public async Task<LaunchOutcome> LaunchAsync(LaunchPlan plan, CancellationToken cancellationToken)
    {
        try
        {
            if (plan.Worktree is not null && !plan.ReuseExistingWorktree)
            {
                await CreateWorktreeAsync(plan, plan.Worktree, cancellationToken);
            }
        }
        catch (GitException failure)
        {
            return new LaunchOutcome(LaunchOutcomeKind.Failed, JobId: null, failure.Message);
        }

        StartResult start = await claude.StartBackgroundAsync(plan.Directory, plan.Name, plan.OpeningPrompt, cancellationToken);
        if (start.Outcome != StartOutcome.Started || start.JobId is null)
        {
            LaunchOutcomeKind kind = start.Outcome == StartOutcome.WorkspaceNotTrusted ? LaunchOutcomeKind.WorkspaceNotTrusted : LaunchOutcomeKind.Failed;

            return new LaunchOutcome(kind, JobId: null, start.Message);
        }

        await store.RecordRecentRepositoryAsync(plan.Repository.Path);
        await RecordDescriptionAsync(plan, start.JobId, cancellationToken);

        return new LaunchOutcome(LaunchOutcomeKind.Started, start.JobId, start.Message);
    }

    private async Task CreateWorktreeAsync(LaunchPlan plan, SiblingWorktree worktree, CancellationToken cancellationToken)
    {
        string repository = plan.Repository.Path;
        string baseRef = plan.Base == WorktreeBase.RemoteDefault
            ? await git.DefaultRemoteBranchAsync(repository, cancellationToken) ?? "HEAD"
            : "HEAD";

        await git.AddWorktreeAsync(repository, worktree.Path, worktree.Branch, baseRef, cancellationToken);
        await copier.CopyAsync(repository, worktree.Path, cancellationToken);
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(worktree.Path, worktree.Branch, repository, clock.GetUtcNow()));
    }

    private async Task RecordDescriptionAsync(LaunchPlan plan, string jobId, CancellationToken cancellationToken)
    {
        if (plan.Description is null)
        {
            return;
        }

        IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(cancellationToken);
        Session? started = sessions.FirstOrDefault(session => session.JobId == jobId);
        if (started is not null)
        {
            await store.WriteDescriptionAsync(started.SessionId, plan.Description);
        }
    }
}