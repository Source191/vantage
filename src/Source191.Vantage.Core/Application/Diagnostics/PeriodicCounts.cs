namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Writes every count to the log's sinks every <see cref="Every"/> while Vantage runs, so a day's figures survive a
/// closed tab or a killed process; the exit write stays as it was. Its timer runs only when <c>VANTAGE_LOG</c> names a
/// file (otherwise it never fires), and it is disposed before the exit write. The timer runs on a pool thread, which <see cref="DiagnosticLog.WriteCounts"/>
/// and the counts' own locks allow.
/// </summary>
public sealed class PeriodicCounts(TimeProvider clock, DiagnosticLog log, PerformanceCounts counts, DiagnosticLogSetting setting) : IDisposable
{
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(10);

    private readonly ITimer timer = Start(clock, log, counts, setting is DiagnosticLogSetting.File ? Every : Timeout.InfiniteTimeSpan);

    public void Dispose() => timer.Dispose();

    private static ITimer Start(TimeProvider clock, DiagnosticLog log, PerformanceCounts counts, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(counts);

        return clock.CreateTimer(_ => log.WriteCounts(counts), null, period, period);
    }
}