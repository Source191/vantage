namespace Source191.Vantage.Domain;

public enum DisplayStateKind
{
    Working,
    NeedsInput,
    Idle,
    Completed,
    Failed,
    Stopped,
    Unknown,
}

public sealed record DisplayState(DisplayStateKind Kind, string Label)
{
    public static readonly DisplayState Working = new(DisplayStateKind.Working, "working");
    public static readonly DisplayState NeedsInput = new(DisplayStateKind.NeedsInput, "needs input");
    public static readonly DisplayState Idle = new(DisplayStateKind.Idle, "idle");
    public static readonly DisplayState Completed = new(DisplayStateKind.Completed, "completed");
    public static readonly DisplayState Failed = new(DisplayStateKind.Failed, "failed");
    public static readonly DisplayState Stopped = new(DisplayStateKind.Stopped, "stopped");

    /// <summary>
    /// Maps the listing's fields to a display state. The listing's documented states are working, blocked, done,
    /// failed and stopped; status (busy, waiting, idle) says what the process is doing right now and wins while it is alive.
    /// </summary>
    public static DisplayState From(SessionListingEntry entry)
    {
        return entry switch
        {
            { ProcessId: null, State: "failed" } => Failed,
            { ProcessId: null } => Stopped,
            { Status: "busy", State: "working" or null } => Working,
            { Status: "waiting" } => NeedsInput,
            { Status: "idle", State: "blocked" } => NeedsInput,
            { Status: "idle", State: "done" } => Completed,
            { Status: "idle", State: "working" or null } => Idle,
            _ => new DisplayState(DisplayStateKind.Unknown, $"{entry.Status}/{entry.State}"),
        };
    }
}