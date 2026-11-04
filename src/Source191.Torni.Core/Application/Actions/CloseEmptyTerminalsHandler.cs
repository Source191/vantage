using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Closes the tabs of plain claude sessions that were never sent a message: nothing to keep, so claude is ended and
/// then the tab's shell is ended cleanly, which makes Windows Terminal close the tab. A session that has been sent a
/// message by the time this runs is left alone and named in the result, as is one whose listed process id now belongs
/// to another process (<see cref="ConfirmedProcess"/>), so a reused id is never ended as a session.
/// </summary>
public sealed class CloseEmptyTerminalsHandler(IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<CloseEmptyTerminals>
{
    public Task<Outcome> HandleAsync(CloseEmptyTerminals effect, CancellationToken cancellationToken)
    {
        List<ListedName> closed = [];
        List<ListedName> ended = [];
        List<SkippedSession> skipped = [];
        List<ListedName> gone = [];
        foreach (Session.Interactive session in effect.Sessions)
        {
            if (transcripts.HasMessages(session.SessionId))
            {
                skipped.Add(new SkippedSession(session.Name));
                continue;
            }

            if (!ConfirmedProcess.TryConfirm(session.ProcessId, processes.StartTimeOf(session.ProcessId), session.StartedAt, out ConfirmedProcess? listed))
            {
                gone.Add(session.Name);
                continue;
            }

            switch (TerminalTab.ShellAbove(listed, processes))
            {
                case TabShell.Above above:
                    processes.Terminate(listed);
                    processes.EndCleanly(above.Shell);
                    closed.Add(session.Name);
                    break;
                case TabShell.TheProcess:
                    processes.EndCleanly(listed);
                    closed.Add(session.Name);
                    break;
                case TabShell.None:
                    processes.Terminate(listed);
                    ended.Add(session.Name);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        return Task.FromResult<Outcome>(new CloseEmptyTerminalsOutcome(closed, ended, skipped, gone));
    }
}