using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Detail;

/// <summary>How a tab's detail pane reads its selected row: the row's key across refreshes, whether it has a screen to read, and the pane's content for one request.</summary>
public interface IDetailSource<in TRow>
{
    bool ReadsScreen { get; }

    object KeyOf(TRow row);

    /// <param name="row"></param>
    /// <param name="rows">How many lines the pane has under its divider, the facts' rows included.</param>
    /// <param name="readScreen">Whether to read the session's screen this time.</param>
    /// <param name="cancellationToken"></param>
    Task<DetailContent> ReadAsync(TRow row, int rows, bool readScreen, CancellationToken cancellationToken);
}

/// <summary>The Sessions tab's pane: a session's facts and its screen, keyed by the session's id.</summary>
public sealed class SessionDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<SessionRow>
{
    private readonly DetailLoader loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public bool ReadsScreen => true;

    public object KeyOf(SessionRow row) => row?.Session.SessionId ?? throw new ArgumentNullException(nameof(row));

    public async Task<DetailContent> ReadAsync(SessionRow row, int rows, bool readScreen, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.SessionAsync(row, Math.Max(rows - Wording.DetailFactRows, 0), readScreen, cancellationToken), clock.GetUtcNow());
}

/// <summary>The Worktrees tab's pane: a worktree's facts and its unpushed commits or changed files, keyed by its folder.</summary>
public sealed class WorktreeDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<WorktreeRow>
{
    private readonly DetailLoader loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public bool ReadsScreen => false;

    public object KeyOf(WorktreeRow row) => row?.Path ?? throw new ArgumentNullException(nameof(row));

    /// <summary>Lists one item fewer than the rows under the facts, so the line saying how many more always fits.</summary>
    public async Task<DetailContent> ReadAsync(WorktreeRow row, int rows, bool readScreen, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.WorktreeAsync(row, Math.Max(rows - Wording.DetailFactRows - 1, 0), cancellationToken), clock.GetUtcNow());
}