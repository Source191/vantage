using Microsoft.Extensions.Logging;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The console session: labels Vantage's own tab, then alternates the two screens in one loop. Each screen keeps its
/// state across switches and hand-offs, so a selection survives a trip to the other view or into an attached session.
/// Effects that need the terminal (attach, the agent view) run here, between two screen runs.
/// </summary>
public sealed class ScreenLoop(
    ISessionsScreen sessionsScreen,
    IWorktreesScreen worktreesScreen,
    OwnTab ownTab,
    ITerminalConsole console,
    TerminalEffects effects,
    ILogger log)
{
    public async Task<int> RunAsync(int processId, CancellationToken cancellationToken)
    {
        TabColour? ownColour = await ownTab.ClaimAsync(processId);
        console.ShowOwnTab(ownColour);

        SessionsViewState sessions = SessionsViewState.Empty;
        WorktreesViewState worktrees = WorktreesViewState.Empty;
        bool showingWorktrees = false;

        while (true)
        {
            ViewEffect? effect;
            if (showingWorktrees)
            {
                WorktreesScreenExit exit = worktreesScreen.Run(worktrees);
                worktrees = exit.State;
                effect = exit.Effect;
            }
            else
            {
                ScreenExit exit = sessionsScreen.Run(sessions);
                sessions = exit.State;
                effect = exit.Effect;
            }

            switch (effect)
            {
                case null or Quit:
                    console.ClearOwnTab();
                    return 0;
                case SwitchToWorktrees:
                    showingWorktrees = true;
                    continue;
                case SwitchToSessions:
                    showingWorktrees = false;
                    continue;
                case JumpToSession jump:
                    showingWorktrees = false;
                    sessions = SessionsView.Reduce(sessions, new SelectSession(jump.SessionId)).State;
                    continue;
                default:
                    await HandOverAsync(effect, ownColour, cancellationToken);
                    continue;
            }
        }
    }

    private async Task HandOverAsync(ViewEffect effect, TabColour? ownColour, CancellationToken cancellationToken)
    {
        if (log.IsEnabled(LogLevel.Information))
        {
            log.HandingOver(effect.ToString() ?? string.Empty);
        }

        await effects.RunAsync(effect, cancellationToken);
        log.ChildReturned();
        console.RecoverAfterChild();
        console.ShowOwnTab(ownColour);
    }
}