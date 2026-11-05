using System.Text.Json.Serialization;

namespace Source191.Torni.ReadmeScene;

/// <summary>One row of <c>claude agents --json --all</c>, in the CLI's own field names; a missing field is left out.</summary>
internal sealed record ListingRow(
    [property: JsonPropertyName("pid")] int? Pid,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("cwd")] string Cwd,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("startedAt")] long StartedAt,
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("waitingFor")] string? WaitingFor);