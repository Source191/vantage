using Source191.Torni.Application;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Torni.Tui;

/// <summary>The worktrees view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class WorktreesScreenDefinition(FolderLabels folders, IDetailSource<WorktreeRow> detail) : IScreenDefinition<WorktreesViewState, WorktreeRow>
{
    public ConsoleTab Tab => ConsoleTab.Worktrees;

    public TabRefresh Refresh => new TabRefresh.Every(TimeSpan.FromSeconds(15));

    public TableDetail<WorktreeRow> Detail => new TableDetail<WorktreeRow>.Pane(detail);

    public string WaitingText => Wording.WaitingText(ConsoleTab.Worktrees);

    public string LoadFailurePrefix => Wording.LoadFailurePrefix(ConsoleTab.Worktrees);

    public string HelpText => Wording.WorktreesHelp;

    public IReadOnlyList<TableColumn<WorktreeRow>> Columns { get; } =
    [
        // The branch and changes are the pane's, so the WORKTREE cell names the repository and the folder's slug only.
        TableColumn<WorktreeRow>.Plain(Wording.WorktreeColumn, (row, _, width) => WhereLabel.For(row.RepositoryName, folders.For(row.Path), inRepository: true).Fit(width), new ColumnFit.Elastic()),
        TableColumn<WorktreeRow>.Plain(Wording.HealthColumn, (row, _, _) => Wording.HealthCell(row.Health.Kind), new ColumnFit.Natural()),
        TableColumn<WorktreeRow>.Plain(Wording.LastCommitColumn, (row, now, _) => row.LastCommitAt is LastCommit.Made made ? Durations.Age(now - made.At) : "-", new ColumnFit.RightAligned()),
        TableColumn<WorktreeRow>.Plain(Wording.SessionColumn, (row, _, _) => row.Session is WorktreeSession.Running running ? Wording.Name(running.Name) : string.Empty, new ColumnFit.Natural()),
    ];
    public Color RowColour(WorktreeRow row)
    {
        return row.Health.Kind switch
        {
            WorktreeHealthKind.Ok => Palette.White,
            WorktreeHealthKind.Locked => Palette.Amber,
            WorktreeHealthKind.Empty => Palette.Grey,
            WorktreeHealthKind.Orphan => Palette.Red,
            WorktreeHealthKind.Broken => Palette.Red,
            WorktreeHealthKind.Elsewhere => Palette.Amber,
        };
    }

    public IReadOnlyList<TextPart> Counts(WorktreesViewState state) => Wording.WorktreeCounts(state.List.Rows);

    public KeyBar Bar(WorktreesViewState state, bool updateKnown) => Wording.WorktreesBar(updateKnown);

    public WorktreesViewState WithMessage(WorktreesViewState state, StatusMessage message) => state with { Message = message };

    public ViewUpdate<WorktreesViewState> Reduce(WorktreesViewState state, ViewInput input) => WorktreesView.Reduce(state, input);
}