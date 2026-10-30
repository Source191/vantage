using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, SessionActivity activity, string claudeExecutable, FolderLabels folders)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    private const int TabColumn = 1;
    private const int WhereColumn = 3;

    /// <summary>The block a Vantage tab's colour is drawn with: two cells, leaving the column's third as space before the state.</summary>
    private const string TabBlock = "██";

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public string Crumb => " <sessions> ";

    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

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
            _ => Durations.Age(now - row.Session.StartedAt),
        };
    }

    /// <summary>The block keeps its tab's colour on every row, the selected one too; a plain claude in its own terminal shows a grey hollow block.</summary>
    public CellPaint? CellPaint(SessionRow row, int column)
    {
        return (column, row) switch
        {
            (TabColumn, { Session: InteractiveSession }) => new CellPaint(Palette.Grey),
            (TabColumn, { HasLiveTab: true, TabColour: TabColour colour }) when Color.TryParse(colour.Hex, out Color? parsed) => new CellPaint(parsed.Value, Always: true),
            _ => null,
        };
    }

    public Color RowColour(SessionRow row) => Palette.ForState(row.Session.State.Kind);

    public string TabTitle(SessionsViewState state) => Wording.TabTitleFor(ConsoleTab.Sessions);

    public IReadOnlyList<TextPart> Counts(SessionsViewState state) => Wording.SessionCounts(state.Rows);

    public KeyBar Bar(SessionsViewState state, bool updateKnown) => Wording.SessionsBar(state, updateKnown);

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
            activity.Observe(before.Rows, loaded.Rows);
        }
    }

    private WhereLabel Where(SessionRow row) => WhereLabel.For(row.Repository, folders.For(row.Session.WorkingDirectory), row.Branch?.Value, row.Checkout is not null);

    /// <summary>A filled cell while a Vantage tab shows the session, a hollow block for a session started outside Vantage in its own tab, nothing otherwise.</summary>
    private static string Swatch(SessionRow row)
    {
        return row switch
        {
            { Session: InteractiveSession } => Wording.TerminalSwatch,
            { HasLiveTab: true } => TabBlock,
            _ => string.Empty,
        };
    }
}