using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

public sealed class StopSessionHandler(IClaudeCli claude, SessionTabs tabs) : IEffectHandler<StopSession>
{
    public async Task<Outcome> HandleAsync(StopSession effect, CancellationToken cancellationToken)
    {
        TabClosure closure = await tabs.CloseAsync(effect.Session.SessionId);
        await claude.StopAsync(effect.Session.JobId.Value, cancellationToken);

        return new StopSessionOutcome(effect.Session.Name, closure == TabClosure.Closed);
    }
}

/// <summary>The session was stopped; it stays listed and can be resumed. TabClosed when a Vantage tab showing it was closed too.</summary>
public sealed record StopSessionOutcome(string Name, bool TabClosed) : Outcome;