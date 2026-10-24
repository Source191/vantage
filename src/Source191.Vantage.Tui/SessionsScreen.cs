using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view: the shared table screen run with the sessions definition.</summary>
public sealed class SessionsScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    StateTransitionNotifier notifier,
    LaunchWorkflow launches,
    SessionActions actions,
    TabOpener tabs,
    WorktreeBase defaultBase,
    TimeProvider clock,
    string context)
    : ISessionsScreen
{
    public ScreenExit Run(SessionsViewState initial)
    {
        TableScreen<SessionsViewState, SessionRow> screen = new(new SessionsScreenDefinition(notifier, launches, actions, tabs, defaultBase), load, clock, context);
        ScreenRun<SessionsViewState> run = screen.Run(initial);

        return new ScreenExit(run.State, run.Exit);
    }
}