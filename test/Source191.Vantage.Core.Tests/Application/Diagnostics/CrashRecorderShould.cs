using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class CrashRecorderShould
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingSink sink = new();
    private readonly ProcessCounts counts = new();
    private readonly FakeCrashFile crashFile = new();
    private readonly DiagnosticLog log;
    private readonly RefreshCounts refreshes;
    private readonly CrashRecorder recorder;

    public CrashRecorderShould()
    {
        log = new DiagnosticLog(clock, [sink]);
        refreshes = new RefreshCounts(clock, log);
        StallCounts stalls = new();
        stalls.Add(TimeSpan.FromMilliseconds(180));
        recorder = new CrashRecorder(log, new PerformanceCounts(counts, refreshes, stalls), crashFile, clock);
        refreshes.Add(RefreshPart.WorktreesScan, TimeSpan.FromMilliseconds(800));
        counts.Add(ChildCommand.For(new ProcessRequest("git", ["status"], WorkingFolder.Inherit)), 0, TimeSpan.FromMilliseconds(12));
    }

    [Fact]
    public async Task WriteTheLogCountsAndCrashThenRethrow()
    {
        log.Record(new ViewShown(ViewKind.Sessions));
        InvalidOperationException crash = new(@"C:\Users\me\source\repos\Northwind.Orders");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => recorder.GuardAsync(() => throw crash));

        thrown.ShouldBeSameAs(crash);
        IReadOnlyList<TimedEvent> recorded = log.Snapshot();
        recorded.Select(timed => timed.Event).ShouldBe([new ViewShown(ViewKind.Sessions), new Failed(FailureSite.Crash, null, ExceptionSummary.From(crash))]);
        crashFile.Written.ShouldBe(string.Join('\n', [.. recorded.Select(DiagnosticLine.Render), "2026-10-29T12:00:00.000Z counts git status exit 0: 1 run, median 12 ms, p95 12 ms, longest 12 ms", "2026-10-29T12:00:00.000Z refreshes worktrees scan: 1 run, median 800 ms, p95 800 ms, longest 800 ms", "2026-10-29T12:00:00.000Z stalls ui thread: 1 stall, median 180 ms, p95 180 ms, longest 180 ms"]) + "\n");
        crashFile.Written!.ShouldNotContain("Northwind");
    }

    [Fact]
    public async Task LeaveTheCrashFileOnANormalReturnAndWriteTheCountsToTheSinks()
    {
        int exitCode = await recorder.GuardAsync(() => Task.FromResult(3));

        exitCode.ShouldBe(3);
        crashFile.Written.ShouldBeNull();
        sink.Lines.ShouldBe(["2026-10-29T12:00:00.000Z counts git status exit 0: 1 run, median 12 ms, p95 12 ms, longest 12 ms", "2026-10-29T12:00:00.000Z refreshes worktrees scan: 1 run, median 800 ms, p95 800 ms, longest 800 ms", "2026-10-29T12:00:00.000Z stalls ui thread: 1 stall, median 180 ms, p95 180 ms, longest 180 ms"]);
    }

    [Fact]
    public async Task LetACancellationThroughWithoutACrashFile()
    {
        await Should.ThrowAsync<OperationCanceledException>(() => recorder.GuardAsync(() => throw new OperationCanceledException()));

        crashFile.Written.ShouldBeNull();
    }

    [Fact]
    public async Task SurviveACrashFileThatCannotBeWritten()
    {
        crashFile.FailWith(new IOException("The disc is full."));
        InvalidOperationException crash = new("boom");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => recorder.GuardAsync(() => throw crash));

        thrown.ShouldBeSameAs(crash);
    }

    [Fact]
    public void RecordAnUnobservedTaskFailureWithoutACrashFile()
    {
        recorder.RecordUnobserved(new AggregateException(new TimeoutException("slow")));

        log.Snapshot().Single().Event.ShouldBeOfType<Failed>().Site.ShouldBe(FailureSite.BackgroundTask);
        crashFile.Written.ShouldBeNull();
    }
}