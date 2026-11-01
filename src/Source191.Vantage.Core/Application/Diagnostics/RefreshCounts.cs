namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// How long each part of a refresh takes, counted by part rather than logged, so the 5-second Sessions refresh does not
/// fill the log; a part slower than <see cref="Slow"/> is also recorded as a line. A cancelled or failed part passes
/// through uncounted (a failure is already recorded where the load fails).
/// </summary>
public sealed class RefreshCounts(TimeProvider clock, DiagnosticLog log)
{
    /// <summary>Clearly worse than the listing's usual 1.6 to 2 seconds, and still under the 5-second Sessions cadence.</summary>
    public static readonly TimeSpan Slow = TimeSpan.FromSeconds(3);

    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly DiagnosticLog log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly DurationSample[] samples = [.. Enum.GetValues<RefreshPart>().Select(_ => new DurationSample())];

    public async Task<T> TimeAsync<T>(RefreshPart part, Func<Task<T>> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        long started = clock.GetTimestamp();
        T result = await run();
        Add(part, clock.GetElapsedTime(started));

        return result;
    }

    /// <summary>Counts a part timed by its caller, recording it as a line when it was slow.</summary>
    public void Add(RefreshPart part, TimeSpan duration)
    {
        samples[(int)part].Add(duration);
        if (duration > Slow)
        {
            log.Record(new RefreshSlow(part, duration));
        }
    }

    /// <summary>Every part that has run at least once, in the enum's order.</summary>
    public IReadOnlyList<RefreshCount> Snapshot() =>
        [.. Enum.GetValues<RefreshPart>().Select(part => new RefreshCount(part, samples[(int)part].Figures())).Where(count => count.Durations.Runs > 0)];
}
