namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// The durations of one kind of run: the number of runs and the longest over every run, and a ring of the latest
/// <see cref="Capacity"/> for the median and 95th percentile, taken by nearest rank. The ring bounds the memory however
/// long Vantage runs (8 KB a sample). Runs arrive from the UI thread and from pool threads.
/// </summary>
public sealed class DurationSample
{
    public const int Capacity = 1000;

    private readonly Lock gate = new();
    private readonly TimeSpan[] latest = new TimeSpan[Capacity];
    private int runs;
    private TimeSpan longest;

    public void Add(TimeSpan duration)
    {
        lock (gate)
        {
            latest[runs % Capacity] = duration;
            runs++;
            longest = duration > longest ? duration : longest;
        }
    }

    public DurationFigures Figures()
    {
        TimeSpan[] sorted;
        int count;
        TimeSpan worst;
        lock (gate)
        {
            count = runs;
            worst = longest;
            sorted = latest[..Math.Min(runs, Capacity)];
        }

        if (count == 0)
        {
            return DurationFigures.None;
        }

        Array.Sort(sorted);

        return new DurationFigures(count, NearestRank(sorted, 50), NearestRank(sorted, 95), worst);
    }

    private static TimeSpan NearestRank(TimeSpan[] sorted, int percentile) => sorted[(int)Math.Ceiling(sorted.Length * percentile / 100.0) - 1];
}
