using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// Parses the output of "claude agents --json --all". Every field is optional because the listing drops
/// id on interactive rows and pid and status on stopped rows.
/// </summary>
public static class ClaudeAgentsListing
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
        if (FolderPath.TryParse(row.Cwd) is not FolderPath workingDirectory || SessionId.TryParse(row.SessionId) is not SessionId sessionId)
        {
            return null;
        }

        string name = row.Name ?? string.Empty;
        DateTimeOffset startedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAt ?? 0);
        DisplayState state = ClaudeStates.From(row.Pid, row.Status, row.State);

        return (row.Kind, row.Pid, JobId.TryParse(row.Id) ?? JobId.Of(sessionId)) switch
        {
            ("interactive", int processId, _) => new InteractiveSession(sessionId, name, workingDirectory, startedAt, state, processId),
            (_, _, JobId jobId) => new BackgroundSession(sessionId, jobId, name, workingDirectory, startedAt, state, row.Pid),
            _ => null,
        };
    }

    private sealed record Row(
        [property: JsonPropertyName("pid")] int? Pid,
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("cwd")] string? Cwd,
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("startedAt")] long? StartedAt,
        [property: JsonPropertyName("sessionId")] string? SessionId,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("waitingFor")] string? WaitingFor,
        [property: JsonPropertyName("state")] string? State);
}