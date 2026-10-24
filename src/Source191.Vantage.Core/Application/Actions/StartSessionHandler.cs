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