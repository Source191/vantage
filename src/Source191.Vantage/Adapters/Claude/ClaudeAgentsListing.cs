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

        return [.. rows.Select(ToSession)];
    }

    private static Session ToSession(Row row)
    {
        SessionListingEntry entry = new(row.Pid, row.Status, row.WaitingFor, row.State);

        return new Session(
            SessionId: row.SessionId ?? string.Empty,
            JobId: row.Id,
            Name: row.Name ?? string.Empty,
            WorkingDirectory: row.Cwd ?? string.Empty,
            Kind: row.Kind == "interactive" ? SessionKind.Interactive : SessionKind.Background,
            StartedAt: DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAt ?? 0),
            State: DisplayState.From(entry),
            ProcessId: row.Pid);
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