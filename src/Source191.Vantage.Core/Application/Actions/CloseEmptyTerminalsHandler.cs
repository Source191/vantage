using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Closes the tabs of plain claude sessions that were never sent a message: nothing to keep, so claude is ended and
/// then the tab's shell is ended cleanly, which makes Windows Terminal close the tab. A session that has been sent a
/// message by the time this runs is left alone and named in the result.
/// </summary>
public sealed class CloseEmptyTerminalsHandler(IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<CloseEmptyTerminals>
{
    public Task<Outcome> HandleAsync(CloseEmptyTerminals effect, CancellationToken cancellationToken)
    {
        List<string> closed = [];
        List<string> ended = [];
        List<SkippedSession> skipped = [];
        foreach (InteractiveSession session in effect.Sessions)
        {
            if (transcripts.HasMessages(session.SessionId.Value))
            {
                skipped.Add(new SkippedSession(session.Name));
                continue;
            }

            int? shell = TerminalTab.ShellOf(session.ProcessId, processes);
            processes.Terminate(session.ProcessId);
            if (shell is null)
            {
                ended.Add(session.Name);
                continue;
            }

            processes.EndCleanly(shell.Value);
            closed.Add(session.Name);
        }

        return Task.FromResult<Outcome>(new CloseEmptyTerminalsOutcome(closed, ended, skipped));
    }

}

/// <summary>Which tabs were closed, which sessions were ended without a tab to close, and which were left alone.</summary>
public sealed record CloseEmptyTerminalsOutcome(IReadOnlyList<string> Closed, IReadOnlyList<string> Ended, IReadOnlyList<SkippedSession> Skipped) : Outcome;

/// <summary>A session close-empty left alone because it has messages by the time it runs.</summary>
public sealed record SkippedSession(string Name);