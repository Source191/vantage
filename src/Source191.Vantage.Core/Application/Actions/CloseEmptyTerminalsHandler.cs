using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Closes the tabs of plain claude sessions that were never sent a message: nothing to keep, so claude is ended and
/// then the tab's shell is ended cleanly, which makes Windows Terminal close the tab. A session with messages or
/// without a process is left alone and named in the result.
/// </summary>
public sealed class CloseEmptyTerminalsHandler(IProcessRegistry processes, ITranscripts transcripts) : IEffectHandler<CloseEmptyTerminals>
{
    public Task<string> HandleAsync(CloseEmptyTerminals effect, CancellationToken cancellationToken)
    {
        List<string> closed = [];
        List<string> ended = [];
        List<string> skipped = [];
        foreach (Session session in effect.Rows.Select(row => row.Session))
        {
            if (session.ProcessId is null)
            {
                skipped.Add($"{session.Name} has no process");
                continue;
            }

            if (transcripts.HasMessages(session.SessionId))
            {
                skipped.Add($"{session.Name} has messages (press b to move it under Vantage)");
                continue;
            }

            int? shell = TerminalTab.ShellOf(session.ProcessId.Value, processes);
            processes.Terminate(session.ProcessId.Value);
            if (shell is null)
            {
                ended.Add(session.Name);
                continue;
            }

            processes.EndCleanly(shell.Value);
            closed.Add(session.Name);
        }

        return Task.FromResult(Summary(closed, ended, skipped));
    }

    private static string Summary(List<string> closed, List<string> ended, List<string> skipped)
    {
        List<string> parts = [];
        if (closed.Count > 0)
        {
            parts.Add($"Closed {closed.Count} {(closed.Count == 1 ? "tab" : "tabs")}: {string.Join(", ", closed)}.");
        }

        if (ended.Count > 0)
        {
            parts.Add($"Ended {ended.Count} {(ended.Count == 1 ? "session" : "sessions")}: {string.Join(", ", ended)} (not in a Windows Terminal tab, so nothing to close).");
        }

        if (closed.Count == 0 && ended.Count == 0)
        {
            return skipped.Count == 0 ? "Nothing to close." : $"Nothing closed: {string.Join("; ", skipped)}.";
        }

        if (skipped.Count > 0)
        {
            parts.Add($"Skipped: {string.Join("; ", skipped)}.");
        }

        return string.Join(' ', parts);
    }
}

/// <summary>Which tabs were closed, which sessions were ended without a tab to close, and which were left alone.</summary>
public sealed record CloseEmptyTerminalsOutcome(IReadOnlyList<string> Closed, IReadOnlyList<string> Ended, IReadOnlyList<SkippedSession> Skipped) : Outcome;

/// <summary>A session close-empty left alone: it has messages, or it has no process.</summary>
public sealed record SkippedSession(string Name, bool HasMessages);