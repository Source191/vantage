using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

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

        return outcome switch
        {
            { Kind: LaunchOutcomeKind.Started, JobId: JobId jobId } => await OpenAsync(effect.Plan.Name.Value, jobId, outcome.Session, cancellationToken),
            { Kind: LaunchOutcomeKind.WorkspaceNotTrusted } => await OpenTrustTabAsync(effect.Plan.Directory, cancellationToken),
            _ => new StartSessionOutcome.Failed(outcome.Message),
        };
    }

    private async Task<StartSessionOutcome> OpenAsync(string name, JobId jobId, BackgroundSession? listed, CancellationToken cancellationToken)
    {
        return listed switch
        {
            null => new StartSessionOutcome.NotListedYet(name, jobId),
            BackgroundSession session => new StartSessionOutcome.StartedAndOpened(name, jobId, await tabs.OpenAsync(session, cancellationToken)),
        };
    }

    private async Task<StartSessionOutcome> OpenTrustTabAsync(FolderPath directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(scripts.Trust(directory), cancellationToken);

        return new StartSessionOutcome.NotTrusted(directory);
    }
}

/// <summary>What starting the wizard's session did, including its tab.</summary>
public abstract record StartSessionOutcome : Outcome
{
    public sealed record StartedAndOpened(string Name, JobId JobId, OpenTabOutcome Tab) : StartSessionOutcome;

    public sealed record NotListedYet(string Name, JobId JobId) : StartSessionOutcome;

    /// <summary>A Claude tab was opened in the directory so the trust prompt can be accepted.</summary>
    public sealed record NotTrusted(FolderPath Directory) : StartSessionOutcome;

    public sealed record Failed(string Message) : StartSessionOutcome;
}