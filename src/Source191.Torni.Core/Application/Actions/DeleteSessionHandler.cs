using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Deletes a session, closes the Torni tab showing it (waiting for its shell, so nothing holds the folder) and, when
/// it lived in a worktree Torni created, removes that worktree under the removal rules through the same remover as
/// the worktrees view. Not a dispatcher handler: the screen confirms first and may ask about the branch afterwards.
/// </summary>
public sealed class DeleteSessionHandler(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees, SessionTabs tabs, IFolders folders, WorktreeRemover remover)
{
    private readonly WorktreeInspector inspector = new(git);

    public async Task<DeleteSessionOutcome> DeleteAsync(Session.Background session, bool forced, CancellationToken cancellationToken)
    {
        try
        {
            await claude.RemoveAsync(session.JobId, cancellationToken);
        }
        catch (ClaudeCliException refusal)
        {
            return new DeleteSessionOutcome.NotDeleted(session.Name, refusal.Message, TabClosed: false);
        }

        bool tabClosed = await tabs.CloseAsync(session.SessionId) == TabClosure.Closed;

        return await RemoveWorktreeAsync(session, forced, tabClosed, cancellationToken);
    }

    private async Task<DeleteSessionOutcome> RemoveWorktreeAsync(Session.Background session, bool forced, bool tabClosed, CancellationToken cancellationToken)
    {
        ListedName name = session.Name;
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(worktree => worktree.Path == session.WorkingDirectory);
        if (created is null)
        {
            return new DeleteSessionOutcome.Deleted(name, tabClosed);
        }

        if (!folders.DirectoryExists(created.Repository.Value))
        {
            return new DeleteSessionOutcome.KeptWorktree(name, new RemovalReason.RepositoryGone(created.Repository), tabClosed);
        }

        WorktreeRow row;
        RemovalDecision decision;
        try
        {
            row = await RowAsync(created, cancellationToken);
            bool merged = await inspector.IsMergedAsync(created.Repository, created.Branch, cancellationToken);
            decision = RemovalDecision.For(row.Facts(branchCreatedHere: true, merged), forced);
        }
        catch (GitException failure)
        {
            return new DeleteSessionOutcome.KeptWorktree(name, new RemovalReason.GitFailed(failure.Message), tabClosed);
        }

        return await remover.CarryOutAsync(row, decision, createdHere: true, cancellationToken) switch
        {
            RemoveWorktreeOutcome.Refused refused => new DeleteSessionOutcome.KeptWorktree(name, refused.Reason, tabClosed),
            RemoveWorktreeOutcome.RemovedAndDeletedBranch removed => new DeleteSessionOutcome.RemovedWorktreeAndBranch(name, removed.Branch, tabClosed),
            RemoveWorktreeOutcome.BranchKept kept => new DeleteSessionOutcome.BranchNotMerged(name, kept.Branch, tabClosed),
            RemoveWorktreeOutcome.InUse inUse => new DeleteSessionOutcome.WorktreeInUse(name, inUse.FolderName, tabClosed),
            RemoveWorktreeOutcome other => new DeleteSessionOutcome.RemovedWorktree(name, other, tabClosed),
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