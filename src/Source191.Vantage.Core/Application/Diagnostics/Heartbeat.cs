namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Watches the UI thread: the console calls <see cref="Beat"/> from a timeout every <see cref="Interval"/>, and each
/// beat is judged by how late it ran after the one before. More than <see cref="Stall"/> late is counted; more than
/// <see cref="Freeze"/> is also recorded, so a sustained slowdown shows in the counts without pushing every other event
/// out of the log. A gap longer than <see cref="Sleep"/> is the machine sleeping and only sets a new baseline, as the
/// first beat does. Terminal.Gui keeps running timeouts under a modal dialog, so a dialog needs no restart.
/// Called on the UI thread only.
/// </summary>
public sealed class Heartbeat(TimeProvider clock, StallCounts stalls, DiagnosticLog log)
{
    public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    /// <summary>About where a key press starts to feel sluggish.</summary>
    public static readonly TimeSpan Stall = TimeSpan.FromMilliseconds(100);

    /// <summary>A freeze anyone notices.</summary>
    public static readonly TimeSpan Freeze = TimeSpan.FromMilliseconds(500);

    public static readonly TimeSpan Sleep = TimeSpan.FromSeconds(30);

    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly StallCounts stalls = stalls ?? throw new ArgumentNullException(nameof(stalls));
    private readonly DiagnosticLog log = log ?? throw new ArgumentNullException(nameof(log));
    private long previous;
    private bool beaten;

    public void Beat()
    {
        long now = clock.GetTimestamp();
        if (beaten)
        {
            Judge(clock.GetElapsedTime(previous, now));
        }

        previous = now;
        beaten = true;
    }

    private void Judge(TimeSpan gap)
    {
        TimeSpan lateness = gap - Interval;
        if (gap > Sleep || lateness <= Stall)
        {
            return;
        }

        stalls.Add(lateness);
        if (lateness > Freeze)
        {
            log.Record(new UiThreadStalled(lateness));
        }
    }
}