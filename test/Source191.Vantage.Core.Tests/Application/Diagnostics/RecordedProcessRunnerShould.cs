using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using System.ComponentModel;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class RecordedProcessRunnerShould
{
    private static readonly ProcessRequest WorktreeList = new("git", ["worktree", "list", "--porcelain"], new WorkingFolder.At(@"C:\Users\me\source\repos\Northwind.Orders"));
    private static readonly ProcessRequest Agents = new(@"C:\Users\me\.local\bin\claude.exe", ["agents", "--json", "--all"], WorkingFolder.Inherit);

    private static readonly DateTimeOffset Written = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, "worktree C:/Users/me", string.Empty));
    private readonly ProcessCounts counts = new();
    private readonly DiagnosticLog log;

    public RecordedProcessRunnerShould()
    {
        log = new DiagnosticLog(clock, []);
    }

    [Fact]
    public async Task CountARunOfFiveSecondsWithoutALine()
    {
        RecordedProcessRunner runner = Runner(TimeSpan.FromSeconds(5));

        await runner.RunAsync(Agents, CancellationToken.None);

        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAFastRunWithoutALine()
    {
        RecordedProcessRunner runner = Runner(TimeSpan.FromMilliseconds(41));

        ProcessResult result = await runner.RunAsync(WorktreeList, CancellationToken.None);

        result.StandardOutput.ShouldBe("worktree C:/Users/me");
        DiagnosticLine.Render(counts, Written).ShouldBe(["2026-11-01T10:00:00.000Z counts git worktree list exit 0: 1 run, median 41 ms, p95 41 ms, longest 41 ms"]);
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordASlowRun()
    {
        RecordedProcessRunner runner = Runner(TimeSpan.FromMilliseconds(6200));

        await runner.RunAsync(Agents, CancellationToken.None);

        log.Snapshot().Select(timed => timed.Event).ShouldBe([new ProcessSlow(ChildCommand.For(Agents), 0, TimeSpan.FromMilliseconds(6200))]);
    }

    [Fact]
    public async Task RecordARunThatCannotStartAndRethrow()
    {
        Win32Exception missing = new(2, @"An error occurred trying to start process 'C:\Users\me\tools\git.exe'");
        processes.Fail(missing);
        RecordedProcessRunner runner = Runner(TimeSpan.Zero);

        Win32Exception thrown = await Should.ThrowAsync<Win32Exception>(() => runner.RunAsync(WorktreeList, CancellationToken.None));

        thrown.ShouldBeSameAs(missing);
        ProcessFailedToStart failed = log.Snapshot().Single().Event.ShouldBeOfType<ProcessFailedToStart>();
        failed.Command.ShouldBe(ChildCommand.For(WorktreeList));
        DiagnosticLine.Render(log.Snapshot().Single()).ShouldNotContain(@"C:\Users\me");
        DiagnosticLine.Render(counts, Written).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordARunThatTimedOutAndRethrow()
    {
        ProcessTimedOutException timedOut = new("claude.exe", TimeSpan.FromSeconds(60));
        processes.Fail(timedOut);
        RecordedProcessRunner runner = Runner(TimeSpan.Zero);

        ProcessTimedOutException thrown = await Should.ThrowAsync<ProcessTimedOutException>(() => runner.RunAsync(Agents, CancellationToken.None));

        thrown.ShouldBeSameAs(timedOut);
        ProcessTimedOut recorded = log.Snapshot().Single().Event.ShouldBeOfType<ProcessTimedOut>();
        recorded.Command.ShouldBe(ChildCommand.For(Agents));
        recorded.Limit.ShouldBe(TimeSpan.FromSeconds(60));
        DiagnosticLine.Render(counts, Written).ShouldBeEmpty();
    }

    [Fact]
    public async Task LetACancellationThroughUnrecorded()
    {
        processes.Fail(new OperationCanceledException());
        RecordedProcessRunner runner = Runner(TimeSpan.Zero);

        await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(WorktreeList, CancellationToken.None));

        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAnInteractiveRunButNeverCallItSlow()
    {
        processes.InteractiveExitCode = 130;
        RecordedProcessRunner runner = Runner(TimeSpan.FromHours(2));

        int exitCode = await runner.RunInteractiveAsync(new ProcessRequest("claude", ["attach", "81cdb0f9"], WorkingFolder.Inherit), CancellationToken.None);

        exitCode.ShouldBe(130);
        DiagnosticLine.Render(counts, Written).ShouldBe(["2026-11-01T10:00:00.000Z counts claude attach exit 130: 1 run, median 7200000 ms, p95 7200000 ms, longest 7200000 ms"]);
        log.Snapshot().ShouldBeEmpty();
    }

    private RecordedProcessRunner Runner(TimeSpan takes)
    {
        return new RecordedProcessRunner(new TakingTime(processes, clock, takes), log, counts, clock);
    }

    /// <summary>A child that takes the given time on the fake clock, as a slow program does on the real one.</summary>
    private sealed class TakingTime(IProcessRunner inner, FakeTimeProvider clock, TimeSpan takes) : IProcessRunner
    {
        public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            ProcessResult result = await inner.RunAsync(request, cancellationToken);
            clock.Advance(takes);
            return result;
        }

        public async Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            int exitCode = await inner.RunInteractiveAsync(request, cancellationToken);
            clock.Advance(takes);
            return exitCode;
        }
    }
}