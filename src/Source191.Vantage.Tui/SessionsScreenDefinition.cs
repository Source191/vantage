using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, SessionActivity activity, string claudeExecutable, FolderLabels folders, IDetailSource<SessionRow> detail)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    private const int TabColumn = 1;
    private const int RepositoryColumn = 3;

    /// <summary>The block a Vantage tab's colour is drawn with: two cells, leaving the column's third as space before the state.</summary>
    private const string TabBlock = "██";

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public IDetailSource<SessionRow>? Detail => detail;

    public string? RowTip(SessionRow row) => null;


    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

    public string HelpText => Wording.SessionsHelp(claudeExecutable);

    public IReadOnlyList<ColumnHelp> Columns => Wording.SessionsColumns;

    public int? ElasticColumn => RepositoryColumn;

    public string Cell(SessionRow row, int column, DateTimeOffset now, int width)
    {
        return column switch
        {
            0 => row.Session.Name,
            TabColumn => Swatch(row),
            2 => $"{Wording.StateGlyph(row.Session.State.Kind)} {row.Session.State.Label}",
            RepositoryColumn => Repository(row).Fit(width),
            4 => Wording.PullRequestCell(row.PullRequest),
            _ => Durations.Age(now - row.Session.StartedAt),
        };
    }

    /// <summary>The block keeps its tab's colour, in the shade the tab shows while Vantage's own tab is selected, on every row, the selected one too; a plain claude in its own terminal shows a grey hollow block.</summary>
    public CellPaint? CellPaint(SessionRow row, int column)
    {
        return (column, row) switch
        {
            (TabColumn, { Session: InteractiveSession }) => new CellPaint(Palette.Grey),
            (TabColumn, { HasLiveTab: true, TabColour: TabColour colour }) when Color.TryParse(TabShade.Unselected(colour), out Color? parsed) => new CellPaint(parsed.Value, Always: true),
            _ => null,
        };
    }

    public Color RowColour(SessionRow row) => Palette.ForState(row.Session.State.Kind);

    public string TabTitle(SessionsViewState state) => Wording.TabTitleFor(ConsoleTab.Sessions);

    public IReadOnlyList<TextPart> Counts(SessionsViewState state) => Wording.SessionCounts(state.Rows, state.AutomatedRuns);

    public KeyBar Bar(SessionsViewState state, bool updateKnown) => Wording.SessionsBar(state, updateKnown);

    public SessionsViewState WithFooter(SessionsViewState state, StatusMessage footer) => state with { Footer = footer };

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

    /// <summary>The repository alone, its middle elided when too long; outside a repository the folder's label, cut at its end. The place and branch are the pane's.</summary>
    private WhereLabel Repository(SessionRow row) => row.Checkout is null
        ? new WhereLabel(null, folders.For(row.Session.WorkingDirectory), null)
        : new WhereLabel(row.Repository, string.Empty, null);

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