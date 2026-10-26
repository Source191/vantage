using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

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
        ProcessRequest request = new("cmd.exe", ["/c", "echo hello from the runner"], WorkingDirectory: null);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.Trim().ShouldBe("hello from the runner");
    }

    [Fact]
    public async Task CaptureStandardErrorAndANonZeroExitCode()
    {
        ProcessRequest request = new("cmd.exe", ["/c", "echo oops 1>&2 & exit 3"], WorkingDirectory: null);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(3);
        result.StandardError.Trim().ShouldBe("oops");
    }

    [Fact]
    public async Task RunTheWorkingDirectoryTheRequestNames()
    {
        string temporary = Path.GetTempPath().TrimEnd('\\');
        ProcessRequest request = new("cmd.exe", ["/c", "cd"], temporary);

        ProcessResult result = await runner.RunAsync(request, TestContext.Current.CancellationToken);

        result.StandardOutput.Trim().ShouldBe(temporary, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task ListTheMachineSessionsThroughTheRealClaudeCli()
    {
        string? executable = ClaudeExecutable.Locate();
        Assert.SkipWhen(executable is null, "Claude Code is not installed on this machine.");

        ClaudeCli cli = new(runner, executable);

        IReadOnlyList<Session> sessions = await cli.ListSessionsAsync(TestContext.Current.CancellationToken);

        sessions.ShouldNotBeNull();
    }
}