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

    /// <summary>A background job with a process and no status yet, the moment after a start or a resume: working, by its own word.</summary>
    public static readonly DisplayState Starting = new(DisplayStateKind.Working, "starting");

    /// <summary>A combination the CLI's words do not map to, shown as its raw words so it is noticed.</summary>
    public static DisplayState Unknown(string label) => new(DisplayStateKind.Unknown, label);
}