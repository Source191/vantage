using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class WorktreesScreenDefinition(FolderLabels folders, IDetailSource<WorktreeRow> detail) : IScreenDefinition<WorktreesViewState, WorktreeRow>
{
    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(15);

    public IDetailSource<WorktreeRow>? Detail => detail;

    public string? RowTip(WorktreeRow row) => null;


    public string WaitingText => "Scanning worktrees";

    public string LoadFailurePrefix => "scan failed";

    public string HelpText => Wording.WorktreesHelp;

    public IReadOnlyList<ColumnHelp> Columns => Wording.WorktreesColumns;

    public int? ElasticColumn => 0;

    public string Cell(WorktreeRow row, int column, DateTimeOffset now, int width)
    {
        return column switch
        {
            // The branch, changes and health are the pane's, so the WORKTREE cell names the repository and the folder's slug only.
            0 => WhereLabel.For(row.RepositoryName, folders.For(row.Path), inRepository: true).Fit(width),
            1 => row.LastCommitAt is null ? "-" : Durations.Age(now - row.LastCommitAt.Value),
            _ => row.Session is WorktreeSession.Running running ? running.Name : string.Empty,
        };
    }

    public CellPaint? CellPaint(WorktreeRow row, int column) => null;

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

    public string TabTitle(WorktreesViewState state) => Wording.TabTitleFor(ConsoleTab.Worktrees);

    public IReadOnlyList<TextPart> Counts(WorktreesViewState state) => Wording.WorktreeCounts(state.Rows);

    public KeyBar Bar(WorktreesViewState state, bool updateKnown) => Wording.WorktreesBar(state, updateKnown);

    public WorktreesViewState WithMessage(WorktreesViewState state, StatusMessage message) => state with { Message = message };

    public (WorktreesViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(WorktreesViewState state, ViewInput input)
    {
        WorktreesViewUpdate update = WorktreesView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(WorktreesViewState before, ViewInput input)
    {
        // Nothing listens to the worktrees view's inputs.
    }
}