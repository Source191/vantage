using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class ProcessRegistryIntegrationShould
{
    private readonly ProcessRegistry registry = new();

    [Fact]
    public void ReportTheStartTimeOfARunningProcessInUtc()
    {
        DateTimeOffset expected = Process.GetCurrentProcess().StartTime.ToUniversalTime();

        DateTimeOffset? actual = registry.StartTimeOf(Environment.ProcessId);

        actual.ShouldNotBeNull();
        actual.Value.ShouldBe(expected);
        actual.Value.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void ReportNothingForAProtectedProcessInsteadOfThrowing()
    {
        Should.NotThrow(() => registry.StartTimeOf(4));
    }

    [Fact]
    public void ReportNothingForAProcessThatDoesNotExist()
    {
        registry.StartTimeOf(int.MaxValue - 7).ShouldBeNull();
        registry.ParentOf(int.MaxValue - 7).ShouldBeNull();
    }

    [Fact]
    public void NameThisProcessAsTheParentOfAChildItStarted()
    {
        using Process child = StartSleeper();
        try
        {
            ProcessParent? parent = registry.ParentOf(child.Id);

            parent.ShouldNotBeNull();
            parent.ProcessId.ShouldBe(Environment.ProcessId);
            parent.Name.ShouldBe(Process.GetCurrentProcess().ProcessName);
        }
        finally
        {
            child.Kill(entireProcessTree: true);
        }
    }

    [Fact]
    public void EndAProcessCleanlySoItsExitCodeIsZero()
    {
        using Process child = StartSleeper();

        registry.EndCleanly(child.Id);

        child.WaitForExit(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        child.ExitCode.ShouldBe(0);
    }

    private static Process StartSleeper()
    {
        ProcessStartInfo start = new("cmd.exe", ["/c", "ping -n 30 127.0.0.1 > nul"]) { UseShellExecute = false, CreateNoWindow = true };

        return Process.Start(start) ?? throw new InvalidOperationException("cmd did not start");
    }
}