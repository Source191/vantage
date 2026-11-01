using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.Launch;

/// <summary>What carrying out a launch plan came to.</summary>
public abstract record LaunchOutcome
{
    /// <summary>The session started under this job id, whether or not the supervisor lists it yet.</summary>
    public abstract record Started(JobId JobId) : LaunchOutcome;

    /// <summary>The supervisor listed the new job by the time the start returned.</summary>
    public sealed record Listed(JobId JobId, BackgroundSession Session) : Started(JobId);

    /// <summary>The supervisor had not listed the new job by the time the start returned.</summary>
    public sealed record NotListedYet(JobId JobId) : Started(JobId);

    /// <summary>The folder's repository has not been trusted interactively yet.</summary>
    public sealed record WorkspaceNotTrusted : LaunchOutcome;

    /// <summary>Git could not create the worktree, or the CLI could not start the session; the message says why.</summary>
    public sealed record Failed(string Message) : LaunchOutcome;
}

/// <summary>
/// Carries out a launch plan: creates the planned worktree when one is wanted, copies the include files,
/// starts the named background session, and records what Vantage needs to remember about it.
/// </summary>
public sealed class LaunchService(IClaudeCli claude, IGit git, IRecentRepositories recent, ICreatedWorktrees created, IWorktreeIncludeCopier copier, TimeProvider clock)
{
    public async Task<LaunchOutcome> LaunchAsync(LaunchPlan plan, CancellationToken cancellationToken)
    {
        try
        {
            if (plan.Place is LaunchPlace.NewWorktree fresh)
            {
                await CreateWorktreeAsync(plan, fresh.Worktree, cancellationToken);
            }
        }
        catch (GitException failure)
        {
            return new LaunchOutcome.Failed(failure.Message);
        }

        StartResult start = await claude.StartBackgroundAsync(plan.Directory.Value, plan.Name, plan.OpeningPrompt, cancellationToken);

        return start switch
        {
            StartResult.Started started => await StartedAsync(plan, started.JobId, cancellationToken),
            StartResult.WorkspaceNotTrusted => new LaunchOutcome.WorkspaceNotTrusted(),
            StartResult.Failed failed => new LaunchOutcome.Failed(failed.Message),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<LaunchOutcome.Started> StartedAsync(LaunchPlan plan, JobId jobId, CancellationToken cancellationToken)
    {
        await recent.RecordRecentRepositoryAsync(plan.Repository.Path.Value);
        BackgroundSession? listed = await ListedSessionAsync(jobId, cancellationToken);

        return listed is null ? new LaunchOutcome.NotListedYet(jobId) : new LaunchOutcome.Listed(jobId, listed);
    }

    private async Task CreateWorktreeAsync(LaunchPlan plan, PlannedWorktree worktree, CancellationToken cancellationToken)
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
        SessionListing listing = await claude.ListSessionsAsync(cancellationToken);

        return listing.Sessions.OfType<BackgroundSession>().FirstOrDefault(session => session.JobId == jobId);
    }
}