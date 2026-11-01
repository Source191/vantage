namespace Source191.Vantage.Domain;

/// <summary>A supervised job: attachable, stoppable and removable by its job id. A stopped job has no process (<see cref="JobProcess.None"/>).</summary>
public sealed record BackgroundSession(
    SessionId SessionId,
    JobId JobId,
    string Name,
    FolderPath WorkingDirectory,
    DateTimeOffset StartedAt,
    DisplayState State,
    JobProcess Process)
    : Session(SessionId, Name, WorkingDirectory, StartedAt, State);