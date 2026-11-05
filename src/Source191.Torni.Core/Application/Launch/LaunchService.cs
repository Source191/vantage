using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application.Launch;

/// <summary>
/// Carries out a launch plan: creates the planned worktree when one is wanted, copies the include files,
/// starts the named background session, and records what the console needs to remember about it.
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
        catch (Exception failure) when (failure is GitException or IOException or UnauthorizedAccessException)
        {
            return new LaunchOutcome.Failed(failure.Message);
        }

        StartResult start = await claude.StartBackgroundAsync(plan.Directory, plan.Name, plan.OpeningPrompt, cancellationToken);

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
        await recent.RecordRecentRepositoryAsync(plan.Repository.Path);
        Session.Background? listed = await ListedSessionAsync(jobId, cancellationToken);

        return listed is null ? new LaunchOutcome.NotListedYet(jobId) : new LaunchOutcome.Listed(jobId, listed);
    }

    private async Task CreateWorktreeAsync(LaunchPlan plan, PlannedWorktree worktree, CancellationToken cancellationToken)
    {
        FolderPath repository = plan.Repository.Path;
        string baseRef = plan.Base == WorktreeBase.RemoteDefault
            ? await git.DefaultRemoteBranchAsync(repository.Value, cancellationToken) ?? "HEAD"
            : "HEAD";

        await git.AddWorktreeAsync(repository.Value, worktree.Path.Value, worktree.Branch.Value, baseRef, cancellationToken);
        await created.RecordCreatedWorktreeAsync(new CreatedWorktree(worktree.Path, worktree.Branch, repository, clock.GetUtcNow()));
        await copier.CopyAsync(repository, worktree.Path, cancellationToken);
    }

    /// <summary>The new session as the supervisor lists it, which is where its full session id comes from.</summary>
    private async Task<Session.Background?> ListedSessionAsync(JobId jobId, CancellationToken cancellationToken)
    {
        SessionListing listing = await claude.ListSessionsAsync(cancellationToken);

        return listing.Sessions.OfType<Session.Background>().FirstOrDefault(session => session.JobId == jobId);
    }
}