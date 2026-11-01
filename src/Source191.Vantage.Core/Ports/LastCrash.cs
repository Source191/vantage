namespace Source191.Vantage.Ports;

/// <summary>What the crash file holds: no crash, or the last crash's text as <c>CrashRecorder</c> wrote it.</summary>
public abstract record LastCrash
{
    public sealed record None : LastCrash;

    public sealed record Recorded(string Text) : LastCrash;
}