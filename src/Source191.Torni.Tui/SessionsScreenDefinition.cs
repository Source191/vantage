using Source191.Torni.Application;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using System.Diagnostics;
using Terminal.Gui.Drawing;

namespace Source191.Torni.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, SessionActivity activity, string claudeExecutable, FolderLabels folders, IDetailSource<SessionRow> detail)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    public ConsoleTab Tab => ConsoleTab.Sessions;

    public TabRefresh Refresh => new TabRefresh.Every(TimeSpan.FromSeconds(5));

    public TableDetail<SessionRow> Detail => new TableDetail<SessionRow>.Pane(detail);

    public string WaitingText => Wording.WaitingText(ConsoleTab.Sessions);

    public string LoadFailurePrefix => Wording.LoadFailurePrefix(ConsoleTab.Sessions);

    public string HelpText => Wording.SessionsHelp(claudeExecutable);

    public IReadOnlyList<TableColumn<SessionRow>> Columns { get; } =
    [
        TableColumn<SessionRow>.Plain(Wording.NameColumn, (row, _, _) => Wording.Name(row.Session.Name), new ColumnFit.Capped(28)),
        new(Wording.TabColumn, (row, _, _) => Swatch(row), new ColumnFit.Natural(), TabPaint),
        TableColumn<SessionRow>.Plain(Wording.StateColumn, (row, _, _) => $"{Wording.StateGlyph(row.Session.State.Kind)} {Wording.StateWord(row.Session.State)}", new ColumnFit.Natural()),
        TableColumn<SessionRow>.Plain(Wording.RepositoryColumn, (row, _, width) => Repository(row, folders).Fit(width), new ColumnFit.Elastic()),
        TableColumn<SessionRow>.Plain(Wording.PullRequestColumn, (row, _, _) => Wording.PullRequestCell(row.PullRequest), new ColumnFit.Natural()),
        TableColumn<SessionRow>.Plain(Wording.AgeColumn, (row, now, _) => Durations.Age(now - row.Session.StartedAt), new ColumnFit.RightAligned()),
    ];

    /// <summary>The block keeps its tab's colour, in the shade the tab shows while Torni's own tab is selected, on every row, the selected one too; a plain claude in its own terminal shows a grey hollow block.</summary>
    private static CellPaint TabPaint(SessionRow row)
    {
        return row switch
        {
            { Session: Session.Interactive } => new CellPaint.Text(Palette.Grey),
            { Tab: LiveTab.Open { Colour: TabColour colour } } when Color.TryParse(TabShade.Unselected(colour), out Color? parsed) => new CellPaint.Fill(parsed.Value),
            _ => new CellPaint.None(),
        };
    }
    public Color RowColour(SessionRow row) => Palette.ForState(row.Session.State.Kind);

    public IReadOnlyList<TextPart> Counts(SessionsViewState state) => Wording.SessionCounts(state.List.Rows, state.AutomatedRuns);

    public KeyBar Bar(SessionsViewState state, bool updateKnown) => Wording.SessionsBar(state, updateKnown);

    public SessionsViewState WithMessage(SessionsViewState state, StatusMessage message) => state with { Message = message };

    public ViewUpdate<SessionsViewState> Reduce(SessionsViewState state, ViewInput input) => SessionsView.Reduce(state, input);

    public void OnInput(SessionsViewState before, ViewInput input)
    {
        if (input is RowsLoaded loaded)
        {
            notifier.Observe(before.List.Rows, loaded.Rows);
            activity.Observe(before.List.Rows, loaded.Rows);
        }
    }

    /// <summary>The repository alone, its middle elided when too long; outside a repository the folder's label, cut at its end. The place and branch are the pane's.</summary>
    private static WhereLabel Repository(SessionRow row, FolderLabels folders) => row.Location switch
    {
        SessionLocation.Outside => new WhereLabel.Outside(folders.For(row.Session.WorkingDirectory)),
        SessionLocation.InRepository inRepository => new WhereLabel.Inside(inRepository.Name, string.Empty),
        _ => throw new UnreachableException(),
    };

    /// <summary>A filled cell while a Torni tab shows the session, a hollow block for a session started outside Torni in its own tab, nothing otherwise.</summary>
    private static string Swatch(SessionRow row)
    {
        return row switch
        {
            { Session: Session.Interactive } => Wording.TerminalSwatch,
            { Tab: LiveTab.Open } => Wording.TabBlock,
            _ => string.Empty,
        };
    }
}