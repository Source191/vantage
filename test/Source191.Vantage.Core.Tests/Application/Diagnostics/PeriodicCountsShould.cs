using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class PeriodicCountsShould
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly RecordingSink sink = new();
    private readonly DiagnosticLog log;
    private readonly PerformanceCounts counts;

    public PeriodicCountsShould()
    {
        log = new DiagnosticLog(clock, [sink]);
        ProcessCounts processes = new();
        processes.Add(ChildCommand.For(new ProcessRequest("git", ["status"], WorkingFolder.Inherit)), 0, TimeSpan.FromMilliseconds(12));
        counts = new PerformanceCounts(processes, new RefreshCounts(clock, log), new StallCounts());
    }

    [Fact]
    public void WriteTheCountsEveryTenMinutes()
    {
        using PeriodicCounts periodic = new(clock, log, counts);

        AdvanceMinutes(25);

        sink.Lines.ShouldBe([
            "2026-11-01T09:10:00.000Z counts git status exit 0: 1 run, median 12 ms, p95 12 ms, longest 12 ms",
            "2026-11-01T09:20:00.000Z counts git status exit 0: 1 run, median 12 ms, p95 12 ms, longest 12 ms",
        ]);
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void StopWritingOnceDisposed()
    {
        PeriodicCounts periodic = new(clock, log, counts);
        AdvanceMinutes(5);

        periodic.Dispose();
        AdvanceMinutes(20);

        sink.Lines.ShouldBeEmpty();
    }

    private void AdvanceMinutes(int minutes)
    {
        foreach (int _ in Enumerable.Range(0, minutes))
        {
            clock.Advance(TimeSpan.FromMinutes(1));
        }
    }
}
