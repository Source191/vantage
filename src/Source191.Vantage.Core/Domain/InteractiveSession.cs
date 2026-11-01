namespace Source191.Vantage.Domain;

/// <summary>Plain claude in a tab of its own: it always has a process, and Vantage can only end it or move it under the supervisor.</summary>
public sealed record InteractiveSession(
    SessionId SessionId,
    string Name,
    FolderPath WorkingDirectory,
    DateTimeOffset StartedAt,
    DisplayState State,
    int ProcessId)
    : Session(SessionId, Name, WorkingDirectory, StartedAt, State);