namespace Source191.Vantage.Domain;

public enum SessionKind
{
    Background,
    Interactive,
}

public sealed record Session(
    string SessionId,
    string? JobId,
    string Name,
    FolderPath WorkingDirectory,
    SessionKind Kind,
    DateTimeOffset StartedAt,
    DisplayState State,
    int? ProcessId = null);