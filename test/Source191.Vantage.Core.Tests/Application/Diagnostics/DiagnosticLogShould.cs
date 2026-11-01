using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class DiagnosticLogShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider clock = new(Noon);

    [Fact]
    public void KeepTheLatest500()
    {
        DiagnosticLog log = new(clock, []);

        foreach (int index in Enumerable.Range(0, 501))
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            log.Record(new ViewShown(index % 2 == 0 ? ViewKind.Sessions : ViewKind.Worktrees));
        }

        IReadOnlyList<TimedEvent> snapshot = log.Snapshot();
        snapshot.Count.ShouldBe(500);
        snapshot[0].At.ShouldBe(Noon.AddSeconds(2));
        snapshot[^1].At.ShouldBe(Noon.AddSeconds(501));
    }

    [Fact]
    public void StampEachEventWithTheClock()
    {
        DiagnosticLog log = new(clock, []);

        log.Record(new ViewShown(ViewKind.Sessions));

        log.Snapshot().ShouldBe([new TimedEvent(Noon, new ViewShown(ViewKind.Sessions))]);
    }

    [Fact]
    public void WriteEachLineToEverySink()
    {
        RecordingSink first = new();
        RecordingSink second = new();
        DiagnosticLog log = new(clock, [first, second]);

        log.Record(new ViewShown(ViewKind.Sessions));

        string line = DiagnosticLine.Render(log.Snapshot().Single());
        first.Lines.ShouldBe([line]);
        second.Lines.ShouldBe([line]);
    }

    [Fact]
    public async Task AcceptEventsFromManyThreads()
    {
        DiagnosticLog log = new(clock, []);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            foreach (int index in Enumerable.Range(0, 50))
            {
                log.Record(new ViewShown(ViewKind.Sessions));
            }
        })));

        log.Snapshot().Count.ShouldBe(400);
    }

    [Fact]
    public void WriteTheCountsToTheSinksWithoutKeepingThem()
    {
        RecordingSink sink = new();
        DiagnosticLog log = new(clock, [sink]);
        ProcessCounts counts = new();
        counts.Add(ChildCommand.For(new Ports.ProcessRequest("git", ["worktree", "list", "--porcelain"], WorkingFolder.Inherit)), 0, TimeSpan.FromMilliseconds(41));

        log.WriteCounts(new PerformanceCounts(counts, new RefreshCounts(clock, log), new StallCounts()));

        sink.Lines.ShouldBe(["2026-10-29T12:00:00.000Z counts git worktree list exit 0: 1 run, median 41 ms, p95 41 ms, longest 41 ms"]);
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteEveryRefreshPartsCount()
    {
        RecordingSink sink = new();
        DiagnosticLog log = new(clock, [sink]);
        RefreshCounts refreshes = new(clock, log);
        await refreshes.TimeAsync(RefreshPart.SessionsListing, () => Taking(TimeSpan.FromMilliseconds(1700)));
        await refreshes.TimeAsync(RefreshPart.SessionsListing, () => Taking(TimeSpan.FromMilliseconds(2200)));
        await refreshes.TimeAsync(RefreshPart.WorktreesScan, () => Taking(TimeSpan.FromMilliseconds(800)));
        clock.Advance(TimeSpan.FromMinutes(10));

        log.WriteCounts(new PerformanceCounts(new ProcessCounts(), refreshes, new StallCounts()));

        sink.Lines.ShouldBe([
            "2026-10-29T12:10:04.700Z refreshes sessions listing: 2 runs, median 1700 ms, p95 2200 ms, longest 2200 ms",
            "2026-10-29T12:10:04.700Z refreshes worktrees scan: 1 run, median 800 ms, p95 800 ms, longest 800 ms",
        ]);
        log.Snapshot().ShouldBeEmpty();
    }

    private async Task<int> Taking(TimeSpan duration)
    {
        await Task.Yield();
        clock.Advance(duration);

        return 0;
    }
}