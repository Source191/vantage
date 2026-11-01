using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session, which only <c>vantage here</c> runs.</summary>
public sealed class TerminalEffects(IClaudeCli claude, ITerminalConsole console)
{
    /// <summary>Whether <see cref="RunAsync"/> has a case for the effect type; the routing fact asks this for every effect that leaves the screen.</summary>
    public static bool Handles(Type effectType) => effectType == typeof(AttachToSession);

    /// <summary>Attaches with the detach hint shown; the hint is erased however the attach ends, and a failure propagates.</summary>
    public async Task RunAsync(AttachToSession attach, CancellationToken cancellationToken)
    {
        console.ShowReturnHint(Wording.ReturnHint);
        try
        {
            await claude.AttachAsync(attach.JobId.Value, cancellationToken);
        }
        finally
        {
            console.RemoveReturnHint();
        }
    }
}