using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Stops the background job, then closes a Vantage tab showing it, if one is open.</summary>
public sealed class StopSessionHandler(IClaudeCli claude, SessionTabs tabs) : IEffectHandler<StopSession>
{
    public async Task<Outcome> HandleAsync(StopSession effect, CancellationToken cancellationToken)
    {
        await claude.StopAsync(effect.Session.JobId.Value, cancellationToken);
        TabClosure closure = await tabs.CloseAsync(effect.Session.SessionId);

        return new StopSessionOutcome(effect.Session.Name, closure == TabClosure.Closed);
    }
}