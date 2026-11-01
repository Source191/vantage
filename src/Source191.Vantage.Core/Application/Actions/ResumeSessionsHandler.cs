using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Resumes the given sessions under the supervisor, each in its own directory, opens a Vantage tab for each one the CLI continues, and says which could not be. Resuming
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
        foreach (BackgroundSession session in effect.Sessions)
        {
            if (!transcripts.HasMessages(session.SessionId.Value))
            {
                notes.Add(new ResumeNote.NeverSentAMessage(session.Name));
                continue;
            }

            await claude.RemoveAsync(session.JobId.Value, cancellationToken);

            // A listed name SessionName refuses is resumed unnamed.
            ResumeResult result = SessionName.TryParse(session.Name, out SessionName? name)
                ? await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken)
                : await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, cancellationToken);
            if (!result.Continued)
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