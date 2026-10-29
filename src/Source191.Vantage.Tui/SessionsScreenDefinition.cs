using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, string claudeExecutable, FolderLabels folders)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public string Crumb => " <sessions> ";

    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

    public string HeaderNames => "Vantage:\nClaude:\nRoots:\nWorktree root:\nSessions:\nNeed input:";

    public IReadOnlyList<KeyHelp> Keys => Wording.SessionsKeys;

    public string HelpText => Wording.SessionsHelp(claudeExecutable);

    public IReadOnlyList<ColumnHelp> Columns => Wording.SessionsColumns;

    public string Cell(SessionRow row, int column, DateTimeOffset now)
    {
        return column switch
        {
            0 => row.Session.Name,
            1 => row.Session.State.Label,
            2 => row.Repository,
            3 => folders.For(row.Session.WorkingDirectory),
            4 => row.Branch?.Value ?? "-",
            5 => Wording.PullRequestCell(row.PullRequest),
            6 => Durations.Age(now - row.Session.StartedAt),
            _ => Tab(row),
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

    /// <summary>"terminal" marks a session started outside Vantage: it lives in its own tab and cannot be attached to.</summary>
    private static string Tab(SessionRow row)
    {
        return row switch
        {
            { Session: InteractiveSession } => "terminal",
            { HasLiveTab: true } => "open",
            _ => string.Empty,
        };
    }
}