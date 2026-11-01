using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Deletes a session, closes the Vantage tab showing it (waiting for its shell, so nothing holds the folder) and, when
/// it lived in a worktree Vantage created, removes that worktree under the removal rules through the same remover as
/// the worktrees view. Not a dispatcher handler: the screen confirms first and may ask about the branch afterwards.
/// </summary>
public sealed class DeleteSessionHandler(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees, SessionTabs tabs, IFolders folders, CurrentSettings settings)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly WorktreeRemover remover = new(git, createdWorktrees, folders, settings);

    public async Task<DeleteSessionOutcome> DeleteAsync(BackgroundSession session, bool forced, CancellationToken cancellationToken)
    {
        await claude.RemoveAsync(session.JobId.Value, cancellationToken);
        bool tabClosed = await tabs.CloseAsync(session.SessionId) == TabClosure.Closed;

        return await RemoveWorktreeAsync(session, forced, cancellationToken) with { TabClosed = tabClosed };
    }

    private async Task<DeleteSessionOutcome> RemoveWorktreeAsync(BackgroundSession session, bool forced, CancellationToken cancellationToken)
    {
        string name = session.Name;
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(worktree => worktree.Path == session.WorkingDirectory);
        if (created is null)
        {
            return new DeleteSessionOutcome.Deleted(name);
        }

        if (!folders.DirectoryExists(created.Repository.Value))
        {
            return new DeleteSessionOutcome.KeptWorktree(name, new RemovalReason.RepositoryGone(created.Repository));
        }

        WorktreeRow row;
        RemovalDecision decision;
        try
        {
            row = await RowAsync(created, cancellationToken);
            bool merged = await inspector.IsMergedAsync(created.Repository, created.Branch, cancellationToken);
            decision = RemovalDecision.For(row.Facts(branchCreatedByVantage: true, merged), forced);
        }
        catch (GitException failure)
        {
            return new DeleteSessionOutcome.KeptWorktree(name, new RemovalReason.GitFailed(failure.Message));
        }

        return await remover.CarryOutAsync(row, decision, createdByVantage: true, cancellationToken) switch
        {
            RemoveWorktreeOutcome.Refused refused => new DeleteSessionOutcome.KeptWorktree(name, refused.Reason),
            RemoveWorktreeOutcome.RemovedAndDeletedBranch removed => new DeleteSessionOutcome.RemovedWorktreeAndBranch(name, removed.Branch),
            RemoveWorktreeOutcome.BranchKept kept => new DeleteSessionOutcome.BranchNotMerged(name, kept.Branch),
            RemoveWorktreeOutcome.InUse inUse => new DeleteSessionOutcome.WorktreeInUse(name, inUse.FolderName),
            RemoveWorktreeOutcome other => new DeleteSessionOutcome.RemovedWorktree(name, other),
        };
    }

    /// <summary>
    /// The worktree as the worktrees view would list it, judged by git's listing and the folder on disc together: a
    /// folder that exists is never taken to be gone, and for any health but ok its files are counted, so a worktree
    /// whose .git file has gone, or that git lists under another spelling of its path, is kept while it holds work.
    /// </summary>
    private async Task<WorktreeRow> RowAsync(CreatedWorktree created, CancellationToken cancellationToken)
    {
        bool folderExists = folders.DirectoryExists(created.Path.Value);
        WorktreeHealth health = await inspector.HealthAsync(created.Repository, created.Path, folderExists, cancellationToken);
        bool healthy = health.Kind == WorktreeHealthKind.Ok;
        WorktreeCounts counts = healthy
            ? await inspector.CountAsync(created.Path, cancellationToken)
            : new WorktreeCounts(0, 0, new LastCommit.Unknown());
        int leftoverFiles = healthy ? 0 : LeftoverFiles.Count(folders, created.Path);

        return new WorktreeRow(created.Repository, created.Path, new Head.OnBranch(created.Branch), health, counts.DirtyFiles, counts.UnpushedCommits, counts.LastCommitAt, new WorktreeSession.None(), leftoverFiles, OwnerExists: true, folderExists);
    }
}