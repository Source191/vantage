using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    private static readonly string[] Columns = ["NAME", "STATE", "REPOSITORY", "BRANCH", "WT", "AGE", "TAB", "DESCRIPTION"];

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public string Crumb => " <sessions> ";

    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

    public string HeaderNames => "Machine:\nClaude:\nRoots:\nSessions:\nNeed input:";

    public string Keys => "<enter>\n<o>\n<n>\n<r>\n<x>\n<d>";

    public string KeyNames => "Attach\nOpen in tab\nNew session\nRename\nStop\nDelete";

    public string Keys2 => "<R>\n<C>\n<a>\n<w>\n</>\n<?>\n<q>";

    public string KeyNames2 => "Resume stopped\nClose empty tabs\nAgent view\nWorktrees\nFilter\nHelp\nQuit";

    public string HelpText => Wording.SessionsHelp;

    public IReadOnlyList<string> ColumnNames => Columns;

    public string Cell(SessionRow row, int column, DateTimeOffset now)
    {
        return column switch
        {
            0 => row.Session.Name,
            1 => row.Session.State.Label,
            2 => row.Repository,
            3 => row.Branch ?? "-",
            4 => row.IsWorktree ? "yes" : string.Empty,
            5 => Durations.Age(now - row.Session.StartedAt),
            6 => Tab(row),
            _ => row.Description ?? string.Empty,
        };
    }

    public Color RowColour(SessionRow row)
    {
        return Palette.ForState(row.Session.State.Kind);
    }

    /// <summary>The sessions line (48 columns at most, so it never wraps into the "Need input" row) and the needs-input count.</summary>
    public string ContextLines(SessionsViewState state)
    {
        const int width = 48;
        IEnumerable<string> byState = state.Rows
            .Where(row => row.Session.State.Kind != DisplayStateKind.NeedsInput)
            .GroupBy(row => row.Session.State.Label)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key}");
        string summary = state.Rows.Count == 0 ? "none" : $"{state.Rows.Count} ({string.Join(", ", byState)})";
        string fitted = summary.Length <= width ? summary : summary[..(width - 1)] + "…";

        return $"{fitted}\n{state.NeedsInputCount}";
    }

    public SessionsViewState WithFooter(SessionsViewState state, Footer footer)
    {
        return state with { Footer = footer };
    }

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

    /// <summary>"terminal" marks a session started outside Vantage: it lives in its own tab and cannot be attached to.</summary>
    private static string Tab(SessionRow row)
    {
        return row switch
        {
            { Session.Kind: SessionKind.Interactive } => "terminal",
            { HasLiveTab: true } => "open",
            _ => string.Empty,
        };
    }
}