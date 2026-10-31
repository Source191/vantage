using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Deletes a session, closes the Vantage tab showing it (waiting for its shell, so nothing holds the folder) and, when
/// it lived in a worktree Vantage created, removes that worktree under the removal rules through the same remover as
/// the worktrees view. Not a dispatcher handler: the screen confirms first and may ask about the branch afterwards.
/// </summary>
public sealed class DeleteSessionHandler(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees, SessionTabs tabs, IFolders folders)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly WorktreeRemover remover = new(git, createdWorktrees, folders);

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

        WorktreeRow row = await RowAsync(created, cancellationToken);
        bool merged = await inspector.IsMergedAsync(created.Repository, created.Branch, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(branchCreatedByVantage: true, merged), forced);

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
    /// The worktree as the worktrees view would list it. Git's listing is the source of truth: a prunable entry means the
    /// folder or its .git has gone, so there is nothing for git status to count and the health decides.
    /// </summary>
    private async Task<WorktreeRow> RowAsync(CreatedWorktree created, CancellationToken cancellationToken)
    {
        WorktreeHealth health = await inspector.HealthAsync(created.Repository, created.Path, cancellationToken);
        WorktreeCounts counts = health.Kind == WorktreeHealthKind.Ok
            ? await inspector.CountAsync(created.Path, cancellationToken)
            : new WorktreeCounts(0, 0, null);

        return new WorktreeRow(created.Repository, created.Path, new Head.OnBranch(created.Branch), health, counts.DirtyFiles, counts.UnpushedCommits, counts.LastCommitAt, new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: true, FolderExists: folders.DirectoryExists(created.Path.Value));
    }
}

/// <summary>What deleting a session did, and in one case the unmerged branch the user is asked about. TabClosed when a Vantage tab showing it was closed.</summary>
public abstract record DeleteSessionOutcome : Outcome
{
    public bool TabClosed { get; init; }

    public sealed record Deleted(string Name) : DeleteSessionOutcome;

    public sealed record KeptWorktree(string Name, RemovalReason Reason) : DeleteSessionOutcome;

    public sealed record RemovedWorktreeAndBranch(string Name, BranchName Branch) : DeleteSessionOutcome;

    public sealed record BranchNotMerged(string Name, BranchToConfirm Branch) : DeleteSessionOutcome;

    /// <summary>The worktree went another way than through git with its branch: its leftover folder deleted, perhaps pruned.</summary>
    public sealed record RemovedWorktree(string Name, RemoveWorktreeOutcome Worktree) : DeleteSessionOutcome;

    /// <summary>The session is deleted; its worktree folder could not be, because a tab or program still has it open.</summary>
    public sealed record WorktreeInUse(string Name, string FolderName) : DeleteSessionOutcome;
}