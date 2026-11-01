using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Detail;

/// <summary>How a tab's detail pane reads its selected row: the lines its facts can take, the row's key across refreshes, and the pane's content.</summary>
public interface IDetailSource<in TRow>
{
    /// <summary>The most lines under the divider this tab's detail can take, so the pane is always that tall.</summary>
    int Rows { get; }

    object KeyOf(TRow row);

    Task<DetailContent> ReadAsync(TRow row, CancellationToken cancellationToken);
}

/// <summary>The Sessions tab's pane: a session's facts, keyed by the session's id.</summary>
public sealed class SessionDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<SessionRow>
{
    private readonly DetailLoader loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public int Rows => Wording.DetailFactRows;

    public object KeyOf(SessionRow row) => row?.Session.SessionId ?? throw new ArgumentNullException(nameof(row));

    public async Task<DetailContent> ReadAsync(SessionRow row, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.SessionAsync(row, cancellationToken), clock.GetUtcNow(), TimeZoneInfo.Utc);
}

/// <summary>The Worktrees tab's pane: a worktree's facts and up to five of its unpushed commits or changed files, keyed by its folder.</summary>
public sealed class WorktreeDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<WorktreeRow>
{
    private readonly DetailLoader loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>The facts, the listed items and the line saying how many more.</summary>
    public int Rows => Wording.DetailFactRows + Wording.DetailListedItems + 1;

    public object KeyOf(WorktreeRow row) => row?.Path ?? throw new ArgumentNullException(nameof(row));

    public async Task<DetailContent> ReadAsync(WorktreeRow row, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.WorktreeAsync(row, Wording.DetailListedItems, cancellationToken), clock.GetUtcNow(), TimeZoneInfo.Utc);
}