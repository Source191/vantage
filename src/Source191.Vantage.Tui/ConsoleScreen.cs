using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The console: the tab that is active runs as the shared table screen; a tab switch is applied by
/// <see cref="ConsoleView"/> and the other tab shown, and only the exit or an effect that needs the terminal leaves.
/// </summary>
public sealed class ConsoleScreen(
    Func<CancellationToken, Task<ViewInput>> loadSessions,
    Func<CancellationToken, Task<ViewInput>> scanWorktrees,
    StateTransitionNotifier notifier,
    DialogEffects dialogs,
    EffectDispatcher dispatcher,
    TimeProvider clock,
    ScreenContext context,
    string claudeExecutable,
    FolderLabels folders)
    : IConsoleScreen
{
    private ConsoleTab? shown;

    public ConsoleExit Run(ConsoleState initial)
    {
        ConsoleState state = initial;
        while (true)
        {
            Record(state.Active);
            (ConsoleState after, ViewEffect? exit) = RunActive(state);
            state = after;
            if (exit is ISwitchesTab switching)
            {
                state = ConsoleView.Apply(state, switching);
                continue;
            }

            return new ConsoleExit(state, exit);
        }
    }

    private (ConsoleState State, ViewEffect? Exit) RunActive(ConsoleState state)
    {
        if (state.Active == ConsoleTab.Worktrees)
        {
            TableScreen<WorktreesViewState, WorktreeRow> worktrees = new(new WorktreesScreenDefinition(folders), dialogs, dispatcher, scanWorktrees, clock, context);
            ScreenRun<WorktreesViewState> run = worktrees.Run(state.Worktrees);

            return (state with { Worktrees = run.State }, run.Exit);
        }

        TableScreen<SessionsViewState, SessionRow> sessions = new(new SessionsScreenDefinition(notifier, claudeExecutable, folders), dialogs, dispatcher, loadSessions, clock, context);
        ScreenRun<SessionsViewState> sessionsRun = sessions.Run(state.Sessions);

        return (state with { Sessions = sessionsRun.State }, sessionsRun.Exit);
    }

    /// <summary>Records the tab about to show when it differs from the last one shown, so a re-run after a hand-off is not recorded.</summary>
    private void Record(ConsoleTab tab)
    {
        if (tab == shown)
        {
            return;
        }

        shown = tab;
        context.Diagnostics.Record(new ViewShown(tab switch
        {
            ConsoleTab.Sessions => ViewKind.Sessions,
            ConsoleTab.Worktrees => ViewKind.Worktrees,
        }));
    }
}
