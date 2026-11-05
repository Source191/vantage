using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Moves a session started as plain claude in another tab under the supervisor: end its process, then resume the
/// same conversation with --bg and its name. Its tab's shell is found before claude ends (the parent chain cannot be
/// read afterwards) and ended cleanly once the resume continued, so the old tab closes; a failed resume keeps the shell
/// at its prompt in the session's folder. A session that has never been sent a message has no transcript, so there is
/// nothing to resume and its process is left alone; so is a process that is no longer the session's
/// (<see cref="ConfirmedProcess"/>), because ending it would end something else.
/// </summary>
public sealed class AdoptSessionHandler(IClaudeCli claude, IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<AdoptSession>
{
    public async Task<Outcome> HandleAsync(AdoptSession effect, CancellationToken cancellationToken)
    {
        Session.Interactive session = effect.Session;
        if (!transcripts.HasMessages(session.SessionId))
        {
            return new AdoptSessionOutcome.NoMessages(session.Name);
        }

        if (!ConfirmedProcess.TryConfirm(session.ProcessId, processes.StartTimeOf(session.ProcessId), session.StartedAt, out ConfirmedProcess? listed))
        {
            return new AdoptSessionOutcome.ProcessGone(session.Name);
        }

        // The tab's shell, found while claude is alive. When claude is the tab's root process, killing it leaves the tab open.
        TabShell shell = TerminalTab.ShellAbove(listed, processes);
        processes.Terminate(listed);
        ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, session.Name, cancellationToken);

        if (result is not ResumeResult.Continued)
        {
            return new AdoptSessionOutcome.Failed(session.Name, result.Message);
        }

        if (shell is not TabShell.Above above)
        {
            return new AdoptSessionOutcome.Moved(session.Name, TabClosed: false);
        }

        processes.EndCleanly(above.Shell);

        return new AdoptSessionOutcome.Moved(session.Name, TabClosed: true);
    }
}