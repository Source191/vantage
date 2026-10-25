using Source191.Vantage.Domain;
using System.Text.Json;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// Parses the output of "claude agents --json --all". Every field is optional because the listing drops
/// id on interactive rows and pid and status on stopped rows.
/// </summary>
public static class ClaudeAgentsListing
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Session> Parse(string json)
    {
        IReadOnlyList<Row> rows = JsonSerializer.Deserialize<IReadOnlyList<Row>>(json, Options) ?? [];

        return [.. rows.Select(ToSession).OfType<Session>()];
    }

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
            return new InteractiveSession(sessionId, name, workingDirectory, startedAt, state, processId);
        }

        return JobId.TryParse(row.Id, out JobId? jobId) || JobId.TryParse(sessionId, out jobId)
            ? new BackgroundSession(sessionId, jobId, name, workingDirectory, startedAt, state, row.Pid)
            : null;
    }

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