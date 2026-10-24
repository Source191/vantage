using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

public sealed class StopSessionHandler(IClaudeCli claude) : IEffectHandler<StopSession>
{
    public async Task<Outcome> HandleAsync(StopSession effect, CancellationToken cancellationToken)
    {
        await claude.StopAsync(effect.Row.Session.JobId!.Value, cancellationToken);

        return new StopSessionOutcome(effect.Row.Session.Name);
    }
}

/// <summary>The session was stopped; it stays listed and can be resumed.</summary>
public sealed record StopSessionOutcome(string Name) : Outcome;