using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Starts the session the launch wizard planned, from either view. When asked it opens the new session's tab; when the
/// workspace is not trusted it opens a plain Claude tab there so the trust prompt can be accepted.
/// </summary>
public sealed class StartSessionHandler(LaunchService service, OpenTabHandler tabs, IWindowsTerminal terminal, string claudeExecutable)
    : IEffectHandler<StartSession>
{
    public async Task<Outcome> HandleAsync(StartSession effect, CancellationToken cancellationToken)
    {
        LaunchOutcome outcome = await service.LaunchAsync(effect.Plan, cancellationToken);

        return outcome switch
        {
            { Kind: LaunchOutcomeKind.Started, JobId: string jobId } => await StartedAsync(effect, jobId, outcome.Session, cancellationToken),
            { Kind: LaunchOutcomeKind.WorkspaceNotTrusted } => await OpenTrustTabAsync(effect.Plan.Directory, cancellationToken),
            _ => new StartSessionOutcome.Failed(outcome.Message),
        };
    }

    private async Task<StartSessionOutcome> StartedAsync(StartSession effect, string jobId, Session? listed, CancellationToken cancellationToken)
    {
        string name = effect.Plan.Name.Text;

        return (effect.OpenInTab, listed) switch
        {
            (false, _) => new StartSessionOutcome.Started(name, jobId),
            (true, null) => new StartSessionOutcome.NotListedYet(name, jobId),
            (true, Session session) => new StartSessionOutcome.StartedAndOpened(name, jobId, await tabs.OpenAsync(session, cancellationToken)),
        };
    }

    private async Task<StartSessionOutcome> OpenTrustTabAsync(string directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(TrustRecovery.TabFor(directory, claudeExecutable), cancellationToken);

        return new StartSessionOutcome.NotTrusted(new FolderPath(directory));
    }
}

/// <summary>What starting the wizard's session did, including the tab when one was asked for.</summary>
public abstract record StartSessionOutcome : Outcome
{
    public sealed record Started(string Name, string JobId) : StartSessionOutcome;

    public sealed record StartedAndOpened(string Name, string JobId, OpenTabOutcome Tab) : StartSessionOutcome;

    public sealed record NotListedYet(string Name, string JobId) : StartSessionOutcome;

    /// <summary>A Claude tab was opened in the directory so the trust prompt can be accepted.</summary>
    public sealed record NotTrusted(FolderPath Directory) : StartSessionOutcome;

    public sealed record Failed(string Message) : StartSessionOutcome;
}