namespace Source191.Vantage.Domain;

public enum SessionKind
{
    Background,
    Interactive,
}

public sealed record Session(
    SessionId SessionId,
    JobId? JobId,
    string Name,
    FolderPath WorkingDirectory,
    SessionKind Kind,
    DateTimeOffset StartedAt,
    DisplayState State,
    int? ProcessId = null);