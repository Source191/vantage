using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Moves a session started as plain claude in another tab under the supervisor: end its process, then resume the
/// same conversation with --bg and its name. The old tab drops back to its shell prompt. A session that has never
/// been sent a message has no transcript, so there is nothing to resume and its process is left alone.
/// </summary>
public sealed class AdoptSessionHandler(IClaudeCli claude, IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<AdoptSession>
{
    public async Task<string> HandleAsync(AdoptSession effect, CancellationToken cancellationToken)
    {
        Session session = effect.Row.Session;
        if (session.ProcessId is null)
        {
            return $"{session.Name} has no process to move; it may have exited.";
        }

        if (!transcripts.HasMessages(session.SessionId))
        {
            return $"{session.Name} has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.";
        }

        processes.Terminate(session.ProcessId.Value);
        ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, SessionName.TryParse(session.Name), cancellationToken);

        return result.Continued
            ? $"Moved {session.Name} under Vantage; its old tab is now a plain shell."
            : $"Could not move {session.Name} under Vantage: {result.Message}";
    }
}