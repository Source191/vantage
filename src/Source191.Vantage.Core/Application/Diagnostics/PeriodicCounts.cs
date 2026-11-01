namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Writes every count to the log's sinks every <see cref="Every"/> while Vantage runs, so a day's figures survive a
/// closed tab or a killed process; the exit write stays as it was. Started only when <c>VANTAGE_LOG</c> names a file,
/// and disposed before the exit write. The timer runs on a pool thread, which <see cref="DiagnosticLog.WriteCounts"/>
/// and the counts' own locks allow.
/// </summary>
public sealed class PeriodicCounts(TimeProvider clock, DiagnosticLog log, PerformanceCounts counts) : IDisposable
{
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(10);

    private readonly ITimer timer = (clock ?? throw new ArgumentNullException(nameof(clock))).CreateTimer(_ => log.WriteCounts(counts), null, Every, Every);

    public void Dispose() => timer.Dispose();
}