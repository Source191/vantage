using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Runs real child processes. The Claude Code check skips cleanly when the CLI is not installed.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProcessRunnerIntegrationShould
{
    private readonly ProcessRunner runner = new();

    [Fact]
    public async Task CaptureStandardOutputAndTheExitCode()
    {
        ProcessRequest request = new("cmd.exe", ["/c", "echo hello from the runner"], WorkingFolder.Inherit);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.Trim().ShouldBe("hello from the runner");
    }

    [Fact]
    public async Task EndAChildThatOutlivesTheLimitAndReportIt()
    {
        ProcessRunner limited = new(TimeSpan.FromSeconds(1));
        DateTime began = DateTime.Now.AddSeconds(-1);
        ProcessRequest request = new("cmd.exe", ["/c", "ping -n 30 127.0.0.1"], WorkingFolder.Inherit);
        Stopwatch watch = Stopwatch.StartNew();

        ProcessTimedOutException timedOut = await Should.ThrowAsync<ProcessTimedOutException>(() => limited.RunAsync(request, TestContext.Current.CancellationToken));

        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
        timedOut.Message.ShouldBe("cmd.exe did not finish within 1 second and was ended");
        timedOut.FileName.ShouldBe("cmd.exe");
        timedOut.Limit.ShouldBe(TimeSpan.FromSeconds(1));
        PingsStartedSince(began).ShouldBeEmpty();
    }

    [Fact]
    public async Task EndAChildWhenItsCallerCancels()
    {
        DateTime began = DateTime.Now.AddSeconds(-1);
        ProcessRequest request = new("cmd.exe", ["/c", "ping -n 30 127.0.0.1"], WorkingFolder.Inherit);
        using CancellationTokenSource cancel = new(TimeSpan.FromMilliseconds(200));
        Stopwatch watch = Stopwatch.StartNew();

        await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(request, cancel.Token));

        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
        PingsStartedSince(began).ShouldBeEmpty();
    }

    private static List<Process> PingsStartedSince(DateTime began)
    {
        Thread.Sleep(500);

        return [.. Process.GetProcessesByName("ping").Where(ping => ping.StartTime >= began)];
    }

    [Fact]
    public async Task CaptureStandardErrorAndANonZeroExitCode()
    {
        ProcessRequest request = new("cmd.exe", ["/c", "echo oops 1>&2 & exit 3"], WorkingFolder.Inherit);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(3);
        result.StandardError.Trim().ShouldBe("oops");
    }

    [Fact]
    public async Task RunTheWorkingDirectoryTheRequestNames()
    {
        string temporary = Path.GetTempPath().TrimEnd('\\');
        ProcessRequest request = new("cmd.exe", ["/c", "cd"], new WorkingFolder.At(temporary));

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.StandardOutput.Trim().ShouldBe(temporary, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task RunAnInheritingChildInVantagesOwnFolder()
    {
        ProcessRequest request = new("cmd.exe", ["/c", "cd"], WorkingFolder.Inherit);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.StandardOutput.Trim().ShouldBe(Environment.CurrentDirectory.TrimEnd('\\'), StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task ListTheMachineSessionsThroughTheRealClaudeCli()
    {
        string? executable = ClaudeExecutable.Locate() is ClaudeLocation.Found found ? found.Path : null;
        Assert.SkipWhen(executable is null, "Claude Code is not installed on this machine.");

        ClaudeCli cli = new(runner, executable);

        IReadOnlyList<Session> sessions = (await cli.ListSessionsAsync(TestContext.Current.CancellationToken)).Sessions;

        sessions.ShouldNotBeNull();
    }
}