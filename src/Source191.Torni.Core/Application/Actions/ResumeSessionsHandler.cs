using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Resumes the given sessions under the supervisor, each in its own directory, opens a session tab for each one the CLI continues, and says which could not be. Resuming
/// a listed job with flags starts a copy under a new session id, so each job is removed first (its transcript stays
/// on disc), as rename does, and the resume continues the same session id in the same row. A session with no
/// transcript keeps its job, since nothing could bring it back.
/// </summary>
public sealed class ResumeSessionsHandler(IClaudeCli claude, ITranscripts transcripts, OpenTabHandler openTab) : IEffectHandler<ResumeSessions>
{
    public async Task<Outcome> HandleAsync(ResumeSessions effect, CancellationToken cancellationToken)
    {
        int continued = 0;
        List<ResumeNote> notes = [];
        foreach (Session.Background session in effect.Sessions)
        {
            if (!transcripts.HasMessages(session.SessionId))
            {
                notes.Add(new ResumeNote.NeverSentAMessage(session.Name));
                continue;
            }

            try
            {
                await claude.RemoveAsync(session.JobId, cancellationToken);
            }
            catch (ClaudeCliException refusal)
            {
                notes.Add(new ResumeNote.NotRemoved(session.Name, refusal.Message));
                continue;
            }

            ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, session.Name, cancellationToken);
            if (result is not ResumeResult.Continued)
            {
                notes.Add(new ResumeNote.Failed(session.Name, result.Message, session.SessionId));
                continue;
            }

            continued++;
            await openTab.OpenAsync(session, cancellationToken);
        }

        return new ResumeSessionsOutcome(continued, effect.Sessions.Count, notes);
    }
}