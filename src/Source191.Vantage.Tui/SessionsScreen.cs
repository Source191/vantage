using Source191.Vantage.Application;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view: the shared table screen run with the sessions definition.</summary>
public sealed class SessionsScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    StateTransitionNotifier notifier,
    DialogEffects dialogs,
    EffectDispatcher dispatcher,
    TimeProvider clock,
    string context)
    : ISessionsScreen
{
    public ScreenExit Run(SessionsViewState initial)
    {
        TableScreen<SessionsViewState, SessionRow> screen = new(new SessionsScreenDefinition(notifier), dialogs, dispatcher, load, clock, context);
        ScreenRun<SessionsViewState> run = screen.Run(initial);

        return new ScreenExit(run.State, run.Exit);
    }
}