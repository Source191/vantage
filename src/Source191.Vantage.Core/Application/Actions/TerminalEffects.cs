using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session and the native agent view.</summary>
public sealed class TerminalEffects(IClaudeCli claude, ITerminalConsole console)
{
    public async Task RunAsync(ViewEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case AttachToSession attach:
                console.ShowReturnHint();
                await claude.AttachAsync(attach.JobId.Value, cancellationToken);
                console.RemoveReturnHint();
                break;
            case OpenAgentView:
                await claude.OpenAgentViewAsync(cancellationToken);
                break;
            default:
                break;
        }
    }
}