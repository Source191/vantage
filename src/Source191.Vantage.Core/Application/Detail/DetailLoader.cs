using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Detail;

/// <summary>
/// Reads the selected row's detail, off the UI thread, one fact at a time: a fact whose read fails is unavailable on its
/// own, so one failing call never blanks the pane, and nothing here throws into the screen.
/// </summary>
public sealed class DetailLoader(IGit git, ICreatedWorktrees created, SessionActivity activity)
{
    private readonly WorktreeInspector inspector = new(git);

    public async Task<SessionDetail> SessionAsync(SessionRow row, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);
        DetailFact<WorktreeCounts> counts = row.Location is SessionLocation.Outside
            ? new DetailFact<WorktreeCounts>.NotApplicable()
            : await Read(() => inspector.CountAsync(row.Session.WorkingDirectory, cancellationToken));

        return new SessionDetail(row, activity.LastPromptedAt(row.Session.SessionId), counts);
    }

    /// <param name="row"></param>
    /// <param name="listRows">How many commits or changed files the pane has room for.</param>
    /// <param name="cancellationToken"></param>
    public async Task<WorktreeDetail> WorktreeAsync(WorktreeRow row, int listRows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);
        WorktreeOrigin origin = await OriginAsync(row);
        DetailFact<MergeState> merge = row.Head is Head.OnBranch { Branch: BranchName branch } && row.OwnerExists
            ? await Read(() => inspector.MergeStateAsync(row.Repository, branch, cancellationToken))
            : new DetailFact<MergeState>.NotApplicable();
        if (!row.FolderExists)
        {
            return new WorktreeDetail(row, origin, merge, new DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable(), new DetailFact<IReadOnlyList<string>>.Unavailable());
        }

        DetailFact<IReadOnlyList<UnpushedCommit>> commits = row.UnpushedCommits > 0
            ? await Read(() => git.UnpushedCommitsAsync(row.Path.Value, listRows, cancellationToken))
            : new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable();
        DetailFact<IReadOnlyList<string>> changed = row.UnpushedCommits == 0 && row.DirtyFiles > 0
            ? await Read(() => git.ChangedFilesAsync(row.Path.Value, listRows, cancellationToken))
            : new DetailFact<IReadOnlyList<string>>.NotApplicable();

        return new WorktreeDetail(row, origin, merge, commits, changed);
    }

    private async Task<WorktreeOrigin> OriginAsync(WorktreeRow row)
    {
        try
        {
            IReadOnlyList<CreatedWorktree> records = await created.ReadCreatedWorktreesAsync();

            return records.FirstOrDefault(record => record.Path.Equals(row.Path)) is CreatedWorktree record
                ? new WorktreeOrigin.CreatedByVantage(record)
                : new WorktreeOrigin.NotByVantage();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new WorktreeOrigin.NotByVantage();
        }
    }

    private static async Task<DetailFact<T>> Read<T>(Func<Task<T>> read)
    {
        try
        {
            return new DetailFact<T>.Known(await read());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new DetailFact<T>.Unavailable();
        }
    }
}