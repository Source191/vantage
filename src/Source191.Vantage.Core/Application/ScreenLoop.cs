using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The console session: labels Vantage's own tab, then alternates the two screens in one loop. Each screen keeps its
/// state across switches and hand-offs, so a selection survives a trip to the other view or into an attached session.
/// Effects that need the terminal (attach) run here, between two screen runs. The update check starts
/// on a pool thread before the first screen and is never awaited; a failure in it is only recorded in the diagnostic log, as are the view shown and each hand-off.
/// </summary>
public sealed class ScreenLoop(
    ISessionsScreen sessionsScreen,
    IWorktreesScreen worktreesScreen,
    OwnTab ownTab,
    ITerminalConsole console,
    TerminalEffects effects,
    UpdateCheck updates,
    DiagnosticLog log)
{
    /// <summary>The update check started by <see cref="RunAsync"/>, which never faults (it ends cancelled with the console's token); tests await it instead of a delay.</summary>
    public Task UpdateCheckTask { get; private set; } = Task.CompletedTask;

    public async Task<int> RunAsync(int processId, CancellationToken cancellationToken)
    {
        TabColour? ownColour = await ownTab.ClaimAsync(processId);
        console.ShowOwnTab(ownColour);
        UpdateCheckTask = CheckForUpdatesAsync(cancellationToken);

        SessionsViewState sessions = SessionsViewState.Empty;
        WorktreesViewState worktrees = WorktreesViewState.Empty;
        bool showingWorktrees = false;
        ViewKind? shown = null;

        while (true)
        {
            shown = Show(showingWorktrees ? ViewKind.Worktrees : ViewKind.Sessions, shown);
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

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => updates.RunAsync(cancellationToken), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Record(new Failed(FailureSite.UpdateCheck, CodeName.For(typeof(UpdateCheck)), ExceptionSummary.From(exception)));
        }
    }

    private async Task HandOverAsync(ViewEffect effect, TabColour? ownColour, CancellationToken cancellationToken)
    {
        log.Record(new HandingOver(CodeName.For(effect.GetType())));
        await effects.RunAsync(effect, cancellationToken);
        log.Record(new ChildReturned());
        console.RecoverAfterChild();
        console.ShowOwnTab(ownColour);
    }

    /// <summary>Records the view about to run when it differs from the last one shown, so a re-run after a hand-off is not recorded.</summary>
    private ViewKind Show(ViewKind view, ViewKind? shown)
    {
        if (view != shown)
        {
            log.Record(new ViewShown(view));
        }

        return view;
    }
}