using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Launch;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Starts the session the launch wizard planned, from either view, and opens the new session's tab; when the workspace
/// is not trusted it opens a plain Claude tab there so the trust prompt can be accepted.
/// </summary>
public sealed class StartSessionHandler(LaunchService service, OpenTabHandler tabs, IWindowsTerminal terminal, TabScripts scripts)
    : IEffectHandler<StartSession>
{
    public async Task<Outcome> HandleAsync(StartSession effect, CancellationToken cancellationToken)
    {
        LaunchOutcome outcome = await service.LaunchAsync(effect.Plan, cancellationToken);

        string name = effect.Plan.Name.Value;

        return outcome switch
        {
            LaunchOutcome.Listed listed => new StartSessionOutcome.StartedAndOpened(name, listed.JobId, await tabs.OpenAsync(listed.Session, cancellationToken)),
            LaunchOutcome.NotListedYet notListed => new StartSessionOutcome.NotListedYet(name, notListed.JobId),
            LaunchOutcome.WorkspaceNotTrusted => await OpenTrustTabAsync(effect.Plan.Directory, cancellationToken),
            LaunchOutcome.Failed failed => new StartSessionOutcome.Failed(failed.Message),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<StartSessionOutcome> OpenTrustTabAsync(FolderPath directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(scripts.Trust(directory), cancellationToken);

        return new StartSessionOutcome.NotTrusted(directory);
    }
}