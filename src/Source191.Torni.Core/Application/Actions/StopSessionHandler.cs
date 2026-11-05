using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>Stops the background job, then closes a session tab showing it, if one is open. A stop claude refuses leaves the tab open.</summary>
public sealed class StopSessionHandler(IClaudeCli claude, SessionTabs tabs) : IEffectHandler<StopSession>
{
    public async Task<Outcome> HandleAsync(StopSession effect, CancellationToken cancellationToken)
    {
        try
        {
            await claude.StopAsync(effect.Session.JobId, cancellationToken);
        }
        catch (ClaudeCliException refusal)
        {
            return new StopSessionOutcome.NotStopped(effect.Session.Name, refusal.Message);
        }

        TabClosure closure = await tabs.CloseAsync(effect.Session.SessionId);

        return new StopSessionOutcome.Stopped(effect.Session.Name, closure == TabClosure.Closed);
    }
}