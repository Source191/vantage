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
    public async Task<string> HandleAsync(StartSession effect, CancellationToken cancellationToken)
    {
        LaunchOutcome outcome = await service.LaunchAsync(effect.Plan, cancellationToken);

        return outcome.Kind switch
        {
            LaunchOutcomeKind.Started => await StartedAsync(effect, outcome, cancellationToken),
            LaunchOutcomeKind.WorkspaceNotTrusted => await OpenTrustTabAsync(effect.Plan.Directory, cancellationToken),
            _ => $"Could not start: {outcome.Message}",
        };
    }

    private async Task<string> StartedAsync(StartSession effect, LaunchOutcome outcome, CancellationToken cancellationToken)
    {
        string started = $"Started {effect.Plan.Name.Text} ({outcome.JobId}).";

        return (effect.OpenInTab, outcome.Session) switch
        {
            (false, _) => started,
            (true, null) => $"{started} It is not listed yet: press o on it to open a tab.",
            (true, Session session) => $"{started} {await tabs.OpenAsync(session, cancellationToken)}",
        };
    }

    private async Task<string> OpenTrustTabAsync(string directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(TrustRecovery.TabFor(directory, claudeExecutable), cancellationToken);

        return TrustRecovery.Explanation(directory);
    }
}

/// <summary>What starting the wizard's session did, including the tab when one was asked for.</summary>
public abstract record StartSessionOutcome : Outcome
{
    public sealed record Started(string Name, string JobId) : StartSessionOutcome;

    public sealed record StartedAndOpened(string Name, string JobId, OpenTabOutcome Tab) : StartSessionOutcome;

    public sealed record NotListedYet(string Name, string JobId) : StartSessionOutcome;

    /// <summary>A Claude tab was opened in the directory so the trust prompt can be accepted.</summary>
    public sealed record NotTrusted(string Directory) : StartSessionOutcome;

    public sealed record Failed(string Message) : StartSessionOutcome;
}