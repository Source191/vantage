using Source191.Torni.Adapters.Processes;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;
using System.Diagnostics;

namespace Source191.Torni.Infrastructure.Tests;

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
        Should.NotThrow(() => registry.ParentOf(4));
        Should.NotThrow(() => registry.UsageOf(4));
    }

    /// <summary>Every read opens its own handle, so a refresh with a claimed tab per session stays cheap.</summary>
    [Fact]
    public void ReadTwoHundredStartTimesWithinASecond()
    {
        Stopwatch clock = Stopwatch.StartNew();
        for (int round = 0; round < 200; round++)
        {
            registry.StartTimeOf(Environment.ProcessId).ShouldNotBeNull();
        }

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReadTwoHundredParentsWithinASecond()
    {
        Stopwatch clock = Stopwatch.StartNew();
        for (int round = 0; round < 200; round++)
        {
            registry.ParentOf(Environment.ProcessId).ShouldNotBeNull();
        }

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReportNothingForAProcessThatDoesNotExist()
    {
        registry.StartTimeOf(int.MaxValue - 7).ShouldBeNull();
        registry.ParentOf(int.MaxValue - 7).ShouldBeNull();
        registry.UsageOf(int.MaxValue - 7).ShouldBeNull();
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
    public void ListThisProcessAndAChildItStartedInTheSnapshot()
    {
        using Process child = StartSleeper();
        try
        {
            IReadOnlyList<ProcessFacts> snapshot = registry.Snapshot();

            ProcessFacts self = snapshot.Single(process => process.ProcessId == Environment.ProcessId);
            self.Name.ShouldBe(Process.GetCurrentProcess().ProcessName);
            ProcessFacts sleeper = snapshot.Single(process => process.ProcessId == child.Id);
            sleeper.ParentId.ShouldBe(Environment.ProcessId);
            sleeper.Name.ShouldBe("cmd");
        }
        finally
        {
            child.Kill(entireProcessTree: true);
        }
    }

    [Fact]
    public void MeasureAChildItStarted()
    {
        DateTimeOffset before = TimeProvider.System.GetUtcNow().AddMinutes(-1);
        using Process child = StartSleeper();
        try
        {
            ProcessUsage? usage = registry.UsageOf(child.Id);

            usage.ShouldNotBeNull();
            usage.StartedAt.ShouldBeInRange(before, TimeProvider.System.GetUtcNow().AddMinutes(1));
            usage.StartedAt.Offset.ShouldBe(TimeSpan.Zero);
            usage.PrivateMemory.Bytes.ShouldBeGreaterThan(0);
            usage.Label.ShouldBe("ping");
        }
        finally
        {
            child.Kill(entireProcessTree: true);
        }
    }

    /// <summary>A refresh measures a few hundred processes, so one query must cost well under a millisecond, not the twenty that enumerating the table per query costs.</summary>
    [Fact]
    public void MeasureTwoHundredTimesWithinASecond()
    {
        Stopwatch clock = Stopwatch.StartNew();
        for (int round = 0; round < 200; round++)
        {
            registry.UsageOf(Environment.ProcessId).ShouldNotBeNull();
        }

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MeasureThisProcessBelowItsWorkingSet()
    {
        ProcessUsage? usage = registry.UsageOf(Environment.ProcessId);

        usage.ShouldNotBeNull();
        usage.PrivateMemory.Bytes.ShouldBeGreaterThan(0);
        usage.PrivateMemory.Bytes.ShouldBeLessThan(Process.GetCurrentProcess().WorkingSet64);
    }

    [Fact]
    public void EndAProcessCleanlySoItsExitCodeIsZero()
    {
        using Process child = StartSleeper();

        registry.EndCleanly(Confirmed(child));

        child.WaitForExit(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        child.ExitCode.ShouldBe(0);
    }

    [Fact]
    public void EndATreeWhoseChildHasAlreadyGoneWithoutThrowing()
    {
        ProcessStartInfo start = new("cmd.exe", ["/c", "cmd /c exit 0 & ping -n 30 127.0.0.1 > nul"]) { UseShellExecute = false, CreateNoWindow = true };
        using Process parent = Process.Start(start) ?? throw new InvalidOperationException("cmd did not start");

        Should.NotThrow(() => registry.Terminate(Confirmed(parent)));

        parent.WaitForExit(TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    private static ConfirmedProcess Confirmed(Process process) => TestProcesses.Confirmed(process.Id, new DateTimeOffset(process.StartTime.ToUniversalTime()));

    private static Process StartSleeper()
    {
        ProcessStartInfo start = new("cmd.exe", ["/c", "ping.exe -n 30 127.0.0.1 > nul"]) { UseShellExecute = false, CreateNoWindow = true };

        return Process.Start(start) ?? throw new InvalidOperationException("cmd did not start");
    }
}