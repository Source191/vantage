using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Opens the update tab: a running torni.exe locks its own files, so the upgrade runs in a tab that waits for this
/// process to exit first. <see cref="TabScripts.Update"/> says what it runs. Without winget no tab opens and Torni stays.
/// </summary>
public sealed class StartUpdateHandler(IWindowsTerminal terminal, TabScripts scripts, OwnProcess torni) : IEffectHandler<StartUpdate>
{
    public async Task<Outcome> HandleAsync(StartUpdate effect, CancellationToken cancellationToken)
    {
        if (torni.Winget is not ProgramLocation.Found winget)
        {
            return new WingetMissingOutcome(effect.Newest.ToString());
        }

        await terminal.OpenTabAsync(scripts.Update(torni.ProcessId, torni.Home, winget.Path), cancellationToken);

        return new UpdateStartedOutcome(effect.Newest.ToString());
    }
}