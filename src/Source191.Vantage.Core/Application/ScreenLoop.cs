using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>
/// The console session: labels Vantage's own tab, then runs the console screen until it quits, handing it the What's new
/// dialog of a first start after an update to show before any key is read. Nothing else leaves the screen: a session
/// opens in a tab of its own, so the console never hands its terminal to a child. The update check starts on a pool
/// thread before the run and is never awaited; a failure in it, or in deciding What's new, is only recorded in the
/// diagnostic log.
/// </summary>
public sealed class ScreenLoop(
    IConsoleScreen screen,
    OwnTab ownTab,
    ITerminalConsole console,
    UpdateCheck updates,
    WhatsNewOnStart whatsNew,
    DiagnosticLog log)
{
    /// <summary>The update check started by <see cref="RunAsync"/>, which never faults (it ends cancelled with the console's token); tests await it instead of a delay.</summary>
    public Task UpdateCheckTask { get; private set; } = Task.CompletedTask;

    public async Task<int> RunAsync(int processId, CancellationToken cancellationToken)
    {
        ShowOwnTab(await ownTab.ClaimAsync(processId));
        UpdateCheckTask = CheckForUpdatesAsync(cancellationToken);

        WhatsNewAtStart opening = await WhatsNewAsync();

        ConsoleExit exit = screen.Run(ConsoleState.Empty, opening);
        if (exit.Effect is not Quit)
        {
            throw new UnreachableException($"Only quit leaves the console, but {exit.Effect.GetType().Name} did.");
        }

        console.ClearOwnTab();

        return 0;
    }

    private void ShowOwnTab(OwnTabClaim claim)
    {
        switch (claim)
        {
            case OwnTabClaim.Claimed claimed:
                console.ShowOwnTab(claimed.Colour);
                break;
            case OwnTabClaim.Unregistered:
                console.ShowOwnTab();
                break;
            default:
                throw new UnreachableException();
        }
    }

    private async Task<WhatsNewAtStart> WhatsNewAsync()
    {
        try
        {
            return await whatsNew.DecideAsync();
        }
        catch (Exception exception)
        {
            log.Record(new Failed(FailureSite.WhatsNew, CodeName.For(typeof(WhatsNewOnStart)), ExceptionSummary.From(exception)));
            return WhatsNewAtStart.Nothing;
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
}