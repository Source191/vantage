namespace Source191.Torni.Domain;

/// <summary>What a session is doing, as a kind the rules read; the words for it are <c>Wording.StateWord</c>'s, and only an unknown combination carries the CLI's own.</summary>
public abstract record DisplayState(DisplayStateKind Kind)
{
    public static readonly DisplayState Working = new Known(DisplayStateKind.Working);
    public static readonly DisplayState NeedsInput = new Known(DisplayStateKind.NeedsInput);
    public static readonly DisplayState Idle = new Known(DisplayStateKind.Idle);
    public static readonly DisplayState Completed = new Known(DisplayStateKind.Completed);
    public static readonly DisplayState Failed = new Known(DisplayStateKind.Failed);
    public static readonly DisplayState Stopped = new Known(DisplayStateKind.Stopped);

    /// <summary>One of the six states the CLI's words map to; only <see cref="DisplayState"/> builds them.</summary>
    public sealed record Known : DisplayState
    {
        internal Known(DisplayStateKind kind)
            : base(kind)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(kind, DisplayStateKind.Unknown);
        }
    }

    /// <summary>A background job with a process and no status yet, the moment after a start or a resume: working, by its own word.</summary>
    public sealed record Starting() : DisplayState(DisplayStateKind.Working);

    /// <summary>A combination the CLI's words do not map to, kept as its raw words so it is noticed.</summary>
    public sealed record Unknown(string RawWords) : DisplayState(DisplayStateKind.Unknown);
}