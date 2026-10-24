using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session and the native agent view.</summary>
public sealed class TerminalEffects(IClaudeCli claude, TextWriter console)
{
    public async Task RunAsync(ViewEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case AttachToSession attach:
                await console.WriteLineAsync(Wording.ReturnHint);
                await claude.AttachAsync(attach.JobId, cancellationToken);
                break;
            case OpenAgentView:
                await claude.OpenAgentViewAsync(cancellationToken);
                break;
            default:
                break;
        }
    }
}