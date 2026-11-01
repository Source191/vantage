using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Text.Json;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// Parses the output of "claude agents --json --all". Every field is optional because the listing drops
/// id on interactive rows and pid and status on stopped rows.
/// </summary>
public static class ClaudeAgentsListing
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static SessionListing Parse(string json)
    {
        IReadOnlyList<Row> rows = JsonSerializer.Deserialize<IReadOnlyList<Row>>(json, Options) ?? [];

        return new SessionListing(
            [.. rows.Where(row => !IsAutomated(row)).Select(ToSession).OfType<Session>()],
            [.. rows.Where(IsAutomated).Select(ToAutomatedRun).OfType<AutomatedRun>()]);
    }

    /// <summary>
    /// A claude started through the Agent SDK or <c>claude -p</c> is listed as interactive with a process and never has a
    /// status; a claude a person started has one from its first moments (2.1.314: within 130 ms of registering).
    /// </summary>
    private static bool IsAutomated(Row row) => row is { Kind: "interactive", Pid: not null, Status: null };

    private static AutomatedRun? ToAutomatedRun(Row row) =>
        FolderPath.TryParse(row.Cwd, out FolderPath? workingDirectory) && SessionId.TryParse(row.SessionId, out SessionId? sessionId) && row.Pid is int processId
            ? new AutomatedRun(sessionId, workingDirectory, processId)
            : null;

    /// <summary>
    /// A row without a session id or a full working directory is skipped: the CLI always gives both, and one odd row must
    /// not fail the listing. An interactive row has a process; any other row is a background job, whose id is the row's
    /// or, failing that, the session id's first eight characters (a row that has neither is skipped too).
    /// </summary>
    private static Session? ToSession(Row row)
    {
        if (!FolderPath.TryParse(row.Cwd, out FolderPath? workingDirectory) || !SessionId.TryParse(row.SessionId, out SessionId? sessionId))
        {
            return null;
        }

        string name = row.Name ?? string.Empty;
        DateTimeOffset startedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAt ?? 0);
        DisplayState state = ClaudeStates.From(row.Pid, row.Status, row.State);

        if (row is { Kind: "interactive", Pid: int processId })
        {
            return new InteractiveSession(sessionId, name, workingDirectory, startedAt, state, processId) { WaitingFor = WaitingOf(row.WaitingFor) };
        }

        return JobId.TryParse(row.Id, out JobId? jobId) || JobId.TryParse(sessionId, out jobId)
            ? new BackgroundSession(sessionId, jobId, name, workingDirectory, startedAt, state, ProcessOf(row.Pid)) { WaitingFor = WaitingOf(row.WaitingFor) }
            : null;
    }

    private static JobProcess ProcessOf(int? pid) => pid is int id ? new JobProcess.Running(id) : new JobProcess.None();

    private static WaitingReason WaitingOf(string? text) => text is string reason ? new WaitingReason.Given(reason) : new WaitingReason.None();

    private sealed record Row(
        int? Pid,
        string? Id,
        string? Cwd,
        string? Kind,
        long? StartedAt,
        string? SessionId,
        string? Name,
        string? Status,
        string? WaitingFor,
        string? State);
}