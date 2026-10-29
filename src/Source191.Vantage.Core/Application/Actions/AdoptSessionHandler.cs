using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Moves a session started as plain claude in another tab under the supervisor: end its process, then resume the
/// same conversation with --bg and its name. The old tab drops back to its shell prompt. A session that has never
/// been sent a message has no transcript, so there is nothing to resume and its process is left alone; so is a
/// process that is no longer the session's (<see cref="ProcessIdentity"/>), because ending it would end something else.
/// </summary>
public sealed class AdoptSessionHandler(IClaudeCli claude, IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<AdoptSession>
{
    public async Task<Outcome> HandleAsync(AdoptSession effect, CancellationToken cancellationToken)
    {
        InteractiveSession session = effect.Session;
        if (!transcripts.HasMessages(session.SessionId.Value))
        {
            return new AdoptSessionOutcome.NoMessages(session.Name);
        }

        if (!ProcessIdentity.StillTheSession(processes.StartTimeOf(session.ProcessId), session.StartedAt))
        {
            return new AdoptSessionOutcome.ProcessGone(session.Name);
        }

        processes.Terminate(session.ProcessId);
        // The port resumes under a name or under none: a listed name SessionName refuses resumes unnamed, so the bool is not needed.
        _ = SessionName.TryParse(session.Name, out SessionName? name);
        ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken);

        return result.Continued
            ? new AdoptSessionOutcome.Moved(session.Name)
            : new AdoptSessionOutcome.Failed(session.Name, result.Message);
    }
}

/// <summary>What moving a terminal session under the supervisor did.</summary>
public abstract record AdoptSessionOutcome : Outcome
{
    public sealed record NoMessages(string Name) : AdoptSessionOutcome;

    public sealed record Moved(string Name) : AdoptSessionOutcome;

    public sealed record Failed(string Name, string Message) : AdoptSessionOutcome;

    /// <summary>The listed process id now belongs to another process, so nothing was ended or resumed.</summary>
    public sealed record ProcessGone(string Name) : AdoptSessionOutcome;
}