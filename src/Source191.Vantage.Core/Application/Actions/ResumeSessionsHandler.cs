using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Resumes the chosen sessions under the supervisor, each in its own directory, and says which could not be. Resuming
/// a listed job with flags starts a copy under a new session id, so each job is removed first (its transcript stays
/// on disc), as rename does, and the resume continues the same session id in the same row. A session with no
/// transcript keeps its job, since nothing could bring it back.
/// </summary>
public sealed class ResumeSessionsHandler(IClaudeCli claude, ITranscripts transcripts, OpenTabHandler openTab) : IEffectHandler<ResumeSessions>
{
    /// <summary>Failed sessions resume at once; stopped ones are offered as a checklist, because a deliberate stop looks the same.</summary>
    public static ResumePlan Plan(IReadOnlyList<SessionRow> rows)
    {
        return new ResumePlan(
            [.. rows.Where(row => row.Session is BackgroundSession { State.Kind: DisplayStateKind.Failed })],
            [.. rows.Where(row => row.Session is BackgroundSession { State.Kind: DisplayStateKind.Stopped })]);
    }

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

/// <summary>How many of the chosen sessions resumed, and a note for each that did not.</summary>
public sealed record ResumeSessionsOutcome(int Resumed, int Chosen, IReadOnlyList<ResumeNote> Notes) : Outcome;

/// <summary>A session that did not resume, and why.</summary>
public abstract record ResumeNote(string Name)
{
    /// <summary>The session has no transcript, so there is nothing to resume.</summary>
    public sealed record NeverSentAMessage(string Name) : ResumeNote(Name);

    /// <summary>The CLI refused the resume with this message; the job was removed first, so the session id is what brings it back.</summary>
    public sealed record Failed(string Name, string Failure, SessionId SessionId) : ResumeNote(Name);
}