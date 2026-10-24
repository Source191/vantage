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
            [.. rows.Where(row => row.Session.State.Kind == DisplayStateKind.Failed)],
            [.. rows.Where(row => row.Session.State.Kind == DisplayStateKind.Stopped)]);
    }

    public async Task<string> HandleAsync(ResumeSessions effect, CancellationToken cancellationToken)
    {
        int continued = 0;
        List<string> notes = [];
        foreach (Session session in effect.Rows.Select(row => row.Session))
        {
            if (!transcripts.HasMessages(session.SessionId))
            {
                notes.Add($"{session.Name}: never sent a message, so there is nothing to resume; d deletes it");
                continue;
            }

            ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, SessionName.TryParse(session.Name), cancellationToken);
            continued += result.Continued ? 1 : 0;
            if (!result.Continued)
            {
                notes.Add($"{session.Name}: {result.Message}");
            }
        }

        string suffix = notes.Count == 0 ? string.Empty : $" Notes: {string.Join("; ", notes)}";

        return $"Resumed {continued} of {effect.Rows.Count}.{suffix}";
    }
}