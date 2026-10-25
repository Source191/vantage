using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

public enum LaunchOutcomeKind
{
    Started,
    WorkspaceNotTrusted,
    Failed,
}

/// <summary>The listed session is null when the supervisor had not listed the new job by the time the start returned.</summary>
public sealed record LaunchOutcome(LaunchOutcomeKind Kind, JobId? JobId, string Message, BackgroundSession? Session = null);

/// <summary>
/// Carries out a launch plan: creates the sibling worktree when one is wanted, copies the include files,
/// starts the named background session, and records what Vantage needs to remember about it.
/// </summary>
public sealed class LaunchService(IClaudeCli claude, IGit git, IRecentRepositories recent, ICreatedWorktrees created, IWorktreeIncludeCopier copier, TimeProvider clock)
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

        StartResult start = await claude.StartBackgroundAsync(plan.Directory.Value, plan.Name, plan.OpeningPrompt, cancellationToken);
        if (start.Outcome != StartOutcome.Started || start.JobId is null)
        {
            LaunchOutcomeKind kind = start.Outcome == StartOutcome.WorkspaceNotTrusted ? LaunchOutcomeKind.WorkspaceNotTrusted : LaunchOutcomeKind.Failed;

            return new LaunchOutcome(kind, JobId: null, start.Message);
        }

        await recent.RecordRecentRepositoryAsync(plan.Repository.Path.Value);
        JobId? jobId = JobId.TryParse(start.JobId);
        BackgroundSession? started = jobId is null ? null : await ListedSessionAsync(jobId, cancellationToken);

        return new LaunchOutcome(LaunchOutcomeKind.Started, jobId, start.Message, started);
    }

    private async Task CreateWorktreeAsync(LaunchPlan plan, SiblingWorktree worktree, CancellationToken cancellationToken)
    {
        FolderPath repository = plan.Repository.Path;
        string baseRef = plan.Base == WorktreeBase.RemoteDefault
            ? await git.DefaultRemoteBranchAsync(repository.Value, cancellationToken) ?? "HEAD"
            : "HEAD";

        await git.AddWorktreeAsync(repository.Value, worktree.Path.Value, worktree.Branch.Value, baseRef, cancellationToken);
        await copier.CopyAsync(repository.Value, worktree.Path.Value, cancellationToken);
        await created.RecordCreatedWorktreeAsync(new CreatedWorktree(worktree.Path, worktree.Branch, repository, clock.GetUtcNow()));
    }

    /// <summary>The new session as the supervisor lists it, which is where its full session id comes from.</summary>
    private async Task<BackgroundSession?> ListedSessionAsync(JobId jobId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(cancellationToken);

        return sessions.OfType<BackgroundSession>().FirstOrDefault(session => session.JobId == jobId);
    }
}