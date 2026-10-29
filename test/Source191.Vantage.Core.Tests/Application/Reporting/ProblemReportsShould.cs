using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Reporting;

public sealed class ProblemReportsShould
{
    private static readonly Started Start = new(new ToolVersion("0.17.0-preview.6"), new Version(2, 1, 312), new Version(10, 0, 26100), new Version(10, 0, 14), 120, 36);

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly ProcessCounts counts = new();
    private readonly FakeCrashFile crashFile = new();
    private readonly DiagnosticLog log;

    public ProblemReportsShould()
    {
        log = new DiagnosticLog(clock, []);
    }

    [Fact]
    public async Task BuildTheEnvironmentEventsCountsAndCrash()
    {
        crashFile.Write("2026-10-28T09:00:00.000Z view Sessions\n2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException\n");
        log.Record(new ViewShown(ViewKind.Worktrees));
        counts.Add(ChildCommand.For(new ProcessRequest("git", ["status"], null)), 0, TimeSpan.FromMilliseconds(12));
        CurrentSettings settings = TestSettings.Current(@"C:\Users\me\source\repos", @"D:\work");

        ProblemReport report = await new ProblemReports(log, counts, crashFile, Start, settings).BuildAsync();

        report.HasCrash.ShouldBeTrue();
        report.Text.ShouldBe(string.Join('\n',
            "Environment",
            "Vantage 0.17.0-preview.6, Claude Code 2.1.312, Windows 10.0.26100, .NET 10.0.14, terminal 120x36",
            "Settings: editor VsCode, update checks on, 2 repository roots",
            string.Empty,
            "Last crash",
            "2026-10-28T09:00:00.000Z view Sessions",
            "2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException",
            string.Empty,
            "Recent events",
            "2026-10-29T12:00:00.000Z view Worktrees",
            string.Empty,
            "Child program counts",
            "counts git status exit 0: 1 run, longest 12 ms"));
    }

    [Fact]
    public async Task LeaveOutTheCrashHeadingWithoutACrash()
    {
        ProblemReport report = await new ProblemReports(log, counts, crashFile, Start, TestSettings.Current(@"C:\Users\me\source\repos")).BuildAsync();

        report.HasCrash.ShouldBeFalse();
        report.Text.ShouldNotContain("Last crash");
        report.Text.ShouldContain("Settings: editor VsCode, update checks on, 1 repository root");
    }
}