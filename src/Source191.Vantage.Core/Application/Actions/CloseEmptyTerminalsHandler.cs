using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Closes the tabs of plain claude sessions that were never sent a message: nothing to keep, so claude is ended and
/// then the tab's shell is ended cleanly, which makes Windows Terminal close the tab. A session that has been sent a
/// message by the time this runs is left alone and named in the result, as is one whose listed process id now belongs
/// to another process (<see cref="ProcessIdentity"/>), so a reused id is never ended as a session.
/// </summary>
public sealed class CloseEmptyTerminalsHandler(IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<CloseEmptyTerminals>
{
    public Task<Outcome> HandleAsync(CloseEmptyTerminals effect, CancellationToken cancellationToken)
    {
        List<string> closed = [];
        List<string> ended = [];
        List<SkippedSession> skipped = [];
        List<string> gone = [];
        foreach (InteractiveSession session in effect.Sessions)
        {
            if (transcripts.HasMessages(session.SessionId.Value))
            {
                skipped.Add(new SkippedSession(session.Name));
                continue;
            }

            if (!ProcessIdentity.StillTheSession(processes.StartTimeOf(session.ProcessId), session.StartedAt))
            {
                gone.Add(session.Name);
                continue;
            }

            switch (TerminalTab.ShellAbove(session.ProcessId, processes))
            {
                case TabShell.Above above:
                    processes.Terminate(session.ProcessId);
                    processes.EndCleanly(above.ProcessId);
                    closed.Add(session.Name);
                    break;
                case TabShell.TheProcess:
                    processes.EndCleanly(session.ProcessId);
                    closed.Add(session.Name);
                    break;
                case TabShell.None:
                    processes.Terminate(session.ProcessId);
                    ended.Add(session.Name);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        return Task.FromResult<Outcome>(new CloseEmptyTerminalsOutcome(closed, ended, skipped, gone));
    }
}