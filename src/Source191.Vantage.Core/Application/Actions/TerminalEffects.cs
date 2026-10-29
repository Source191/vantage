using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session.</summary>
public sealed class TerminalEffects(IClaudeCli claude, ITerminalConsole console)
{
    public async Task RunAsync(ViewEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case AttachToSession attach:
                console.ShowReturnHint(Wording.ReturnHint);
                await claude.AttachAsync(attach.JobId.Value, cancellationToken);
                console.RemoveReturnHint();
                break;
            default:
                break;
        }
    }
}