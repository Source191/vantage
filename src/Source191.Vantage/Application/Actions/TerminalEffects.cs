using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Actions;

/// <summary>Runs the effects that need the terminal to themselves: attaching to a session and the native agent view.</summary>
public sealed class TerminalEffects(IClaudeCli claude, TextWriter console)
{
    /// <summary>Attach lands on Claude's agent list when the session has no messages yet; that screen leaves with Esc, not Ctrl+Z.</summary>
    private const string ReturnHint = "Ctrl+Z returns to Vantage. If you see the agent list instead, the session has no messages yet: Space sends it a prompt, Esc returns to Vantage.";

    public async Task RunAsync(ViewEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case AttachToSession attach:
                await console.WriteLineAsync(ReturnHint);
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