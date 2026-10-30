using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, string claudeExecutable, FolderLabels folders)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    private const int TabColumn = 1;
    private const int WhereColumn = 3;

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public string Crumb => " <sessions> ";

    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

    public string HeaderNames => "Vantage:\nClaude:\nRoots:\nWorktree root:\nSessions:\nNeed input:";

    public IReadOnlyList<KeyHelp> Keys => Wording.SessionsKeys;

    public string HelpText => Wording.SessionsHelp(claudeExecutable);

    public IReadOnlyList<ColumnHelp> Columns => Wording.SessionsColumns;

    public int? ElasticColumn => WhereColumn;

    public string Cell(SessionRow row, int column, DateTimeOffset now, int width)
    {
        return column switch
        {
            0 => row.Session.Name,
            TabColumn => Swatch(row),
            2 => $"{Wording.StateGlyph(row.Session.State.Kind)} {row.Session.State.Label}",
            WhereColumn => Where(row).Fit(width),
            4 => Wording.PullRequestCell(row.PullRequest),
            5 => Durations.Age(now - row.Session.StartedAt),
            _ => Wording.MemoryCell(row.Memory),
        };
    }

    /// <summary>The swatch takes its tab's colour, or grey for a plain claude in its own terminal; every other cell is the row's colour.</summary>
    public Color? CellColour(SessionRow row, int column)
    {
        return (column, row) switch
        {
            (TabColumn, { Session: InteractiveSession }) => Palette.Grey,
            (TabColumn, { TabColour: TabColour colour }) when Color.TryParse(colour.Hex, out Color? parsed) => parsed,
            _ => null,
        };
    }

    public Color RowColour(SessionRow row) => Palette.ForState(row.Session.State.Kind);

    /// <summary>The sessions line (45 columns at most, so it never wraps into the "Need input" row) and the needs-input count.</summary>
    public string ContextLines(SessionsViewState state)
    {
        const int width = 45;
        string summary = Wording.SessionsSummary(state.Rows);
        string fitted = summary.Length <= width ? summary : summary[..(width - 1)] + "…";

        return $"{fitted}\n{state.NeedsInputCount}";
    }

    public SessionsViewState WithFooter(SessionsViewState state, Footer footer) => state with { Footer = footer };

    public (SessionsViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(SessionsViewState state, ViewInput input)
    {
        ViewUpdate update = SessionsView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(SessionsViewState before, ViewInput input)
    {
        if (input is RowsLoaded loaded)
        {
            notifier.Observe(before.Rows, loaded.Rows);
        }
    }

    private WhereLabel Where(SessionRow row) => WhereLabel.For(row.Repository, folders.For(row.Session.WorkingDirectory), row.Branch?.Value, row.Checkout is not null);

    /// <summary>A solid block while a Vantage tab shows the session, a hollow one for a session started outside Vantage in its own tab, nothing otherwise.</summary>
    private static string Swatch(SessionRow row)
    {
        return row switch
        {
            { Session: InteractiveSession } => Wording.TerminalSwatch,
            { HasLiveTab: true } => Wording.OpenTabSwatch,
            _ => string.Empty,
        };
    }
}