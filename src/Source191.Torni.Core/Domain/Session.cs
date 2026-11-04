namespace Source191.Torni.Domain;

/// <summary>
/// A listed Claude Code session: a <see cref="Background"/> under the supervisor, or an <see cref="Interactive"/>
/// running as plain claude in some tab. The type says which, so code that acts on one kind takes that type and never
/// checks a flag or forgives a null job id.
/// </summary>
public abstract record Session(SessionId SessionId, ListedName Name, FolderPath WorkingDirectory, DateTimeOffset StartedAt, DisplayState State)
{
    /// <summary>What the listing says the session waits for ("permission prompt"), in the CLI's own words; none when it says nothing.</summary>
    public WaitingReason WaitingFor { get; init; } = new WaitingReason.None();

    /// <summary>A supervised job: attachable, stoppable and removable by its job id. A stopped job has no process (<see cref="JobProcess.None"/>).</summary>
    public sealed record Background(
        SessionId SessionId,
        JobId JobId,
        ListedName Name,
        FolderPath WorkingDirectory,
        DateTimeOffset StartedAt,
        DisplayState State,
        JobProcess Process)
        : Session(SessionId, Name, WorkingDirectory, StartedAt, State);

    /// <summary>Plain claude in a tab of its own: it always has a process, and Torni can only end it or move it under the supervisor.</summary>
    public sealed record Interactive(
        SessionId SessionId,
        ListedName Name,
        FolderPath WorkingDirectory,
        DateTimeOffset StartedAt,
        DisplayState State,
        int ProcessId)
        : Session(SessionId, Name, WorkingDirectory, StartedAt, State);
}