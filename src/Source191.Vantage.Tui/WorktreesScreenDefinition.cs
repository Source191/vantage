using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class WorktreesScreenDefinition : IScreenDefinition<WorktreesViewState, WorktreeRow>
{
    private static readonly string[] Columns = ["REPOSITORY", "FOLDER", "BRANCH", "DIRTY", "UNPUSHED", "LAST COMMIT", "SESSION", "HEALTH"];

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(15);

    public string Crumb => " <worktrees> ";

    public string WaitingText => "Scanning worktrees";

    public string LoadFailurePrefix => "scan failed";

    public string HeaderNames => "Machine:\nClaude:\nRoots:\nWorktrees:\nAt risk:";

    public string Keys => "<d>\n<D>\n<p>\n<s>\n<o>\n<enter>";

    public string KeyNames => "Remove\nForce remove\nPrune\nStart session\nOpen shell\nGo to session";

    public string Keys2 => "<w>\n<esc>\n</>\n<?>\n<q>";

    public string KeyNames2 => "Sessions\nSessions\nFilter\nHelp\nQuit";

    public string HelpText => Wording.WorktreesHelp;

    public IReadOnlyList<string> ColumnNames => Columns;

    public string Cell(WorktreeRow row, int column, DateTimeOffset now)
    {
        return column switch
        {
            0 => row.RepositoryName,
            1 => row.FolderName,
            2 => row.Branch?.Value ?? "-",
            3 => Count(row.DirtyFiles),
            4 => Count(row.UnpushedCommits),
            5 => row.LastCommitAt is null ? "-" : Durations.Age(now - row.LastCommitAt.Value),
            6 => row.SessionName ?? string.Empty,
            // The kind only: the reason goes in the footer for the selected row, so a long one is never cut off.
            _ => row.Health.Kind.ToString().ToLowerInvariant(),
        };
    }

    public Color RowColour(WorktreeRow row)
    {
        return row.Health.Kind switch
        {
            WorktreeHealthKind.Ok => Palette.White,
            WorktreeHealthKind.Locked => Palette.Amber,
            WorktreeHealthKind.Empty => Palette.Grey,
            _ => Palette.Red,
        };
    }

    public string ContextLines(WorktreesViewState state)
    {
        IEnumerable<string> byHealth = state.Rows
            .GroupBy(row => row.Health.Kind)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key.ToString().ToLowerInvariant()}");
        string summary = state.Rows.Count == 0 ? "none" : $"{state.Rows.Count} ({string.Join(", ", byHealth)})";
        int dirty = state.Rows.Count(row => row.DirtyFiles > 0);
        int unpushed = state.Rows.Count(row => row.UnpushedCommits > 0);

        return $"{summary}\n{dirty} with changes, {unpushed} with unpushed commits";
    }

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