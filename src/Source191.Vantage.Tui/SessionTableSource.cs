using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>Presents the visible rows to Terminal.Gui's TableView.</summary>
internal sealed class SessionTableSource(IReadOnlyList<SessionRow> rows, DateTimeOffset now) : ITableSource
{
    private static readonly string[] Names = ["NAME", "STATE", "REPOSITORY", "BRANCH", "WT", "AGE", "TAB", "DESCRIPTION"];

    public int Rows => rows.Count;

    public int Columns => Names.Length;

    public string[] ColumnNames => Names;

    public object this[int row, int col]
    {
        get
        {
            SessionRow item = rows[row];

            return col switch
            {
                0 => item.Session.Name,
                1 => item.Session.State.Label,
                2 => item.Repository,
                3 => item.Branch ?? "-",
                4 => item.IsWorktree ? "yes" : string.Empty,
                5 => Age(now - item.Session.StartedAt),
                6 => Tab(item),
                _ => item.Description ?? string.Empty,
            };
        }
    }

    public SessionRow RowAt(int index)
    {
        return rows[index];
    }

    /// <summary>"terminal" marks a session started outside Vantage: it lives in its own tab and cannot be attached to.</summary>
    private static string Tab(SessionRow item)
    {
        return item switch
        {
            { Session.Kind: SessionKind.Interactive } => "terminal",
            { HasLiveTab: true } => "open",
            _ => string.Empty,
        };
    }

    internal static string Age(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return elapsed.TotalHours switch
        {
            < 1 => $"{(int)elapsed.TotalMinutes}m",
            < 24 => $"{(int)elapsed.TotalHours}h{elapsed.Minutes:00}m",
            _ => $"{(int)elapsed.TotalDays}d{elapsed.Hours}h",
        };
    }
}