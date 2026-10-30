using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session.</summary>
public sealed class TerminalEffects(IClaudeCli claude, ITerminalConsole console)
{
    /// <summary>Whether <see cref="RunAsync"/> has a case for the effect type; the routing fact asks this for every effect that leaves the screen.</summary>
    public static bool Handles(Type effectType) => effectType == typeof(AttachToSession);

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