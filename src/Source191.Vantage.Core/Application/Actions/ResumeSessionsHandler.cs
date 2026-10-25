using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Resumes the chosen sessions under the supervisor, each in its own directory, and says which could not be.</summary>
public sealed class ResumeSessionsHandler(IClaudeCli claude, ITranscripts transcripts) : IEffectHandler<ResumeSessions>
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
                notes.Add(new ResumeNote(session.Name, Failure: null));
                continue;
            }

            // The port resumes under a name or under none: a listed name the CLI would refuse resumes unnamed, so the bool is not needed.
            _ = SessionName.TryParse(session.Name, out SessionName? name);
            ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken);
            continued += result.Continued ? 1 : 0;
            if (!result.Continued)
            {
                notes.Add(new ResumeNote(session.Name, result.Message));
            }
        }

        return new ResumeSessionsOutcome(continued, effect.Sessions.Count, notes);
    }
}

/// <summary>How many of the chosen sessions resumed, and a note for each that did not.</summary>
public sealed record ResumeSessionsOutcome(int Resumed, int Chosen, IReadOnlyList<ResumeNote> Notes) : Outcome;

/// <summary>A session that did not resume: the CLI's failure, or null when it was never sent a message.</summary>
public sealed record ResumeNote(string Name, string? Failure);