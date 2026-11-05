using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Detail;

/// <summary>
/// Reads the selected row's detail, off the UI thread, one fact at a time: a fact whose read fails is unavailable on its
/// own, so one failing call never blanks the pane, and nothing here throws into the screen.
/// </summary>
public sealed class DetailLoader(IGit git, ICreatedWorktrees created, SessionActivity activity, DiagnosticLog log)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly OncePerFolder unreadableFacts = new(log, FailureSite.Load, CodeName.For(typeof(DetailLoader)));
    private readonly OncePerFolder unreadableOrigins = new(log, FailureSite.Load, CodeName.For(typeof(DetailLoader)));

    public async Task<SessionDetail> SessionAsync(SessionRow row, CancellationToken cancellationToken)
    {
        DetailFact<WorktreeCounts> counts = row.Location is SessionLocation.Outside
            ? new DetailFact<WorktreeCounts>.NotApplicable()
            : await Read(row.Session.WorkingDirectory, () => inspector.CountAsync(row.Session.WorkingDirectory, cancellationToken));

        return new SessionDetail(row, activity.LastPromptedAt(row.Session.SessionId), counts);
    }

    /// <param name="row">The selected worktree row.</param>
    /// <param name="listRows">How many commits or changed files the pane has room for.</param>
    /// <param name="cancellationToken">Cancels the git reads still running.</param>
    public async Task<WorktreeDetail> WorktreeAsync(WorktreeRow row, int listRows, CancellationToken cancellationToken)
    {
        WorktreeOrigin origin = await OriginAsync(row);
        DetailFact<MergeState> merge = row.Head is Head.OnBranch { Branch: BranchName branch } && row.OwnerExists
            ? await Read(row.Path, () => inspector.MergeStateAsync(row.Repository, branch, cancellationToken))
            : new DetailFact<MergeState>.NotApplicable();
        if (!row.FolderExists)
        {
            return new WorktreeDetail(row, origin, merge, new DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable(), new DetailFact<IReadOnlyList<string>>.Unavailable());
        }

        DetailFact<IReadOnlyList<UnpushedCommit>> commits = row.UnpushedCommits > 0
            ? await Read(row.Path, () => git.UnpushedCommitsAsync(row.Path.Value, listRows, cancellationToken))
            : new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable();
        DetailFact<IReadOnlyList<string>> changed = row.UnpushedCommits == 0 && row.DirtyFiles > 0
            ? await Read(row.Path, () => git.ChangedFilesAsync(row.Path.Value, listRows, cancellationToken))
            : new DetailFact<IReadOnlyList<string>>.NotApplicable();

        return new WorktreeDetail(row, origin, merge, commits, changed);
    }

    private Task<WorktreeOrigin> OriginAsync(WorktreeRow row) => unreadableOrigins.RunAsync<WorktreeOrigin>(
        row.Path,
        async () =>
        {
            IReadOnlyList<CreatedWorktree> records = await created.ReadCreatedWorktreesAsync();

            return records.FirstOrDefault(record => record.Path.Equals(row.Path)) is CreatedWorktree record
                ? new WorktreeOrigin.CreatedHere(record)
                : new WorktreeOrigin.NotCreatedHere();
        },
        IsNotCancellation,
        _ => new WorktreeOrigin.NotCreatedHere());

    private Task<DetailFact<T>> Read<T>(FolderPath folder, Func<Task<T>> read) => unreadableFacts.RunAsync<DetailFact<T>>(
        folder,
        async () => new DetailFact<T>.Known(await read()),
        IsNotCancellation,
        _ => new DetailFact<T>.Unavailable());

    private static bool IsNotCancellation(Exception exception) => exception is not OperationCanceledException;
}