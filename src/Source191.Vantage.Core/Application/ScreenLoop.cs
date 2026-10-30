using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The console session: labels Vantage's own tab, then runs the console screen until it quits. The screen keeps both
/// tabs' state and hands it back on every exit, so a selection survives a trip into an attached session. Effects that
/// need the terminal (attach) run here, between two runs. The update check starts on a pool thread before the first
/// run and is never awaited; a failure in it is only recorded in the diagnostic log, as is each hand-off.
/// </summary>
public sealed class ScreenLoop(
    IConsoleScreen screen,
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

        ConsoleState state = ConsoleState.Empty;
        while (true)
        {
            ConsoleExit exit = screen.Run(state);
            state = exit.State;
            if (exit.Effect is null or Quit)
            {
                console.ClearOwnTab();
                return 0;
            }

            await HandOverAsync(exit.Effect, ownColour, cancellationToken);
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
}