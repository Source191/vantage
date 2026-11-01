namespace Source191.Vantage.Domain;

/// <summary>
/// A listed Claude Code session: a <see cref="BackgroundSession"/> under the supervisor, or an
/// <see cref="InteractiveSession"/> running as plain claude in some tab. The type says which, so code that acts on one
/// kind takes that type and never checks a flag or forgives a null job id.
/// </summary>
public abstract record Session(SessionId SessionId, string Name, FolderPath WorkingDirectory, DateTimeOffset StartedAt, DisplayState State)
{
    /// <summary>What the listing says the session waits for ("permission prompt"), in the CLI's own words; none when it says nothing.</summary>
    public WaitingReason WaitingFor { get; init; } = new WaitingReason.None();
}

/// <summary>A supervised job: attachable, stoppable and removable by its job id. A stopped job has no process (<see cref="JobProcess.None"/>).</summary>
public sealed record BackgroundSession(
    SessionId SessionId,
    JobId JobId,
    string Name,
    FolderPath WorkingDirectory,
    DateTimeOffset StartedAt,
    DisplayState State,
    JobProcess Process)
    : Session(SessionId, Name, WorkingDirectory, StartedAt, State)
{
    public int? ProcessId => Process is JobProcess.Running running ? running.Id : null;
}

/// <summary>Plain claude in a tab of its own: it always has a process, and Vantage can only end it or move it under the supervisor.</summary>
public sealed record InteractiveSession(
    SessionId SessionId,
    string Name,
    FolderPath WorkingDirectory,
    DateTimeOffset StartedAt,
    DisplayState State,
    int ProcessId)
    : Session(SessionId, Name, WorkingDirectory, StartedAt, State);