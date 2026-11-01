using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Moves a session started as plain claude in another tab under the supervisor: end its process, then resume the
/// same conversation with --bg and its name. Its tab's shell is found before claude ends (the parent chain cannot be
/// read afterwards) and ended cleanly once the resume continued, so the old tab closes; a failed resume keeps the shell
/// at its prompt in the session's folder. A session that has never been sent a message has no transcript, so there is
/// nothing to resume and its process is left alone; so is a process that is no longer the session's
/// (<see cref="ProcessIdentity"/>), because ending it would end something else.
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

        // The tab's shell, unless claude was the tab's root process itself and ending it closes the tab already.
        int? shell = TerminalTab.ShellOf(session.ProcessId, processes) is int root && root != session.ProcessId ? root : null;
        processes.Terminate(session.ProcessId);
        // A listed name SessionName refuses is resumed unnamed.
        ResumeResult result = SessionName.TryParse(session.Name, out SessionName? name)
            ? await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken)
            : await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, cancellationToken);

        if (!result.Continued)
        {
            return new AdoptSessionOutcome.Failed(session.Name, result.Message);
        }

        if (shell is int tab)
        {
            processes.EndCleanly(tab);
        }

        return new AdoptSessionOutcome.Moved(session.Name, TabClosed: shell is not null);
    }
}