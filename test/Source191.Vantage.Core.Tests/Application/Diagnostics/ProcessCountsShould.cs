using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class ProcessCountsShould
{
    private static readonly DateTimeOffset Written = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly ChildCommand WorktreeList = ChildCommand.For(new ProcessRequest("git", ["worktree", "list", "--porcelain"], WorkingFolder.Inherit));
    private static readonly ChildCommand WorktreeRemove = ChildCommand.For(new ProcessRequest("git", ["worktree", "remove", "x"], WorkingFolder.Inherit));

    [Fact]
    public void CountRunsByProgramSubcommandAndExitCode()
    {
        ProcessCounts counts = new();

        counts.Add(WorktreeList, 0, TimeSpan.FromMilliseconds(41));
        counts.Add(WorktreeRemove, 128, TimeSpan.FromMilliseconds(220));
        counts.Add(WorktreeList, 0, TimeSpan.FromMilliseconds(90.6));
        counts.Add(WorktreeList, 0, TimeSpan.FromMilliseconds(12));

        DiagnosticLine.Render(counts, Written).ShouldBe([
            "2026-11-01T10:00:00.000Z counts git worktree list exit 0: 3 runs, median 41 ms, p95 90 ms, longest 90 ms",
            "2026-11-01T10:00:00.000Z counts git worktree remove exit 128: 1 run, median 220 ms, p95 220 ms, longest 220 ms",
        ]);
    }

    [Fact]
    public void HoldTheTypicalDurationsOfEachCommand()
    {
        ProcessCounts counts = new();
        ChildCommand agents = ChildCommand.For(new ProcessRequest(@"C:\Users\me\.local\bin\claude.exe", ["agents", "--json"], WorkingFolder.Inherit));

        foreach (int _ in Enumerable.Range(0, 99))
        {
            counts.Add(agents, 0, TimeSpan.FromMilliseconds(1600));
        }

        counts.Add(agents, 0, TimeSpan.FromMilliseconds(4900));

        counts.Snapshot().ShouldBe([new ProcessCount(agents, 0, new DurationFigures(100, TimeSpan.FromMilliseconds(1600), TimeSpan.FromMilliseconds(1600), TimeSpan.FromMilliseconds(4900)))]);
    }

    [Fact]
    public void RenderNothingBeforeAnyRun()
    {
        DiagnosticLine.Render(new ProcessCounts(), Written).ShouldBeEmpty();
    }

    [Fact]
    public async Task CountRunsFromManyThreads()
    {
        ProcessCounts counts = new();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            foreach (int index in Enumerable.Range(0, 50))
            {
                counts.Add(WorktreeList, 0, TimeSpan.FromMilliseconds(index));
            }
        })));

        DiagnosticLine.Render(counts, Written).ShouldBe(["2026-11-01T10:00:00.000Z counts git worktree list exit 0: 400 runs, median 24 ms, p95 47 ms, longest 49 ms"]);
    }
}