using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class WorktreesScreenDefinition(FolderLabels folders) : IScreenDefinition<WorktreesViewState, WorktreeRow>
{
    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(15);

    public string Crumb => " <worktrees> ";

    public string WaitingText => "Scanning worktrees";

    public string LoadFailurePrefix => "scan failed";

    public string HelpText => Wording.WorktreesHelp;

    public IReadOnlyList<ColumnHelp> Columns => Wording.WorktreesColumns;

    public int? ElasticColumn => 0;

    public string Cell(WorktreeRow row, int column, DateTimeOffset now, int width)
    {
        return column switch
        {
            // The branch has its own column here, so the WORKTREE cell names the repository and the folder's slug only.
            0 => WhereLabel.For(row.RepositoryName, folders.For(row.Path), branch: null, inRepository: true).Fit(width),
            1 => row.Branch?.Value ?? "-",
            2 => Wording.PullRequestCell(row.PullRequest),
            3 => Count(row.DirtyFiles),
            4 => Count(row.UnpushedCommits),
            5 => row.LastCommitAt is null ? "-" : Durations.Age(now - row.LastCommitAt.Value),
            6 => row.SessionName ?? string.Empty,
            // The kind only: the reason goes in the footer for the selected row, so a long one is never cut off.
            _ => row.Health.Kind.ToString().ToLowerInvariant(),
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

    public string TabTitle(WorktreesViewState state) => Wording.TabTitleFor(ConsoleTab.Worktrees, state.Loaded ? state.Rows.Count : null);

    public IReadOnlyList<TextPart> Counts(WorktreesViewState state) => Wording.WorktreeCounts(state.Rows);

    public KeyBar Bar(WorktreesViewState state, bool updateKnown) => Wording.WorktreesBar(state, updateKnown);

    public WorktreesViewState WithFooter(WorktreesViewState state, Footer footer) => state with { Footer = footer };

    public (WorktreesViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(WorktreesViewState state, ViewInput input)
    {
        WorktreesViewUpdate update = WorktreesView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(WorktreesViewState before, ViewInput input)
    {
        // Nothing listens to the worktrees view's inputs.
    }

    private static string Count(int number) => number == 0 ? string.Empty : number.ToString(System.Globalization.CultureInfo.InvariantCulture);
}