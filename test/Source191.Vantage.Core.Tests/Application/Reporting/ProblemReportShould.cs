using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Reporting;

public sealed class ProblemReportShould
{
    private static readonly Started Start = new(new ToolVersion("0.17.0-preview.6"), new Version(2, 1, 312), new Version(10, 0, 26100), new Version(10, 0, 14), 120, 36);
    private static readonly ReportTitle Title = ReportTitle.From("Attach froze after Ctrl+Z");

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly ProcessCounts counts = new();
    private readonly FakeCrashFile crashFile = new();
    private readonly DiagnosticLog log;

    public ProblemReportShould()
    {
        log = new DiagnosticLog(clock, []);
    }

    [Fact]
    public async Task FitWithinTheAddressLimit()
    {
        crashFile.Write(string.Join('\n', Enumerable.Range(0, 300).Select(index => $"2026-10-28T09:00:00.000Z view Sessions {index}")) + "\n2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException at ScreenLoop.RunAsync (ScreenLoop.cs:42)\n");
        foreach (int index in Enumerable.Range(0, 500))
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            log.Record(index % 50 == 0 ? new Failed(FailureSite.Effect, CodeName.For(typeof(Quit)), ExceptionSummary.From(new GitException("x"))) : new ViewShown(ViewKind.Sessions));
        }

        foreach (string program in new[] { "status", "log", "branch", "rev-list" })
        {
            counts.Add(ChildCommand.For(new ProcessRequest("git", [program], null)), 0, TimeSpan.FromMilliseconds(5));
            counts.Add(ChildCommand.For(new ProcessRequest("git", [program], null)), 128, TimeSpan.FromMilliseconds(5));
        }

        ProblemReport report = await Reports().BuildAsync();
        string fitted = report.FitWithin(Title, ProblemReport.AddressLimit);

        ProblemReportAddress.Length(Title, fitted).ShouldBeLessThanOrEqualTo(ProblemReport.AddressLimit);
        fitted.ShouldStartWith("Environment\nVantage 0.17.0-preview.6");
        fitted.ShouldContain("failed Crash: InvalidOperationException at ScreenLoop.RunAsync (ScreenLoop.cs:42)");
        fitted.ShouldContain("2026-10-29T12:07:31.000Z failed Effect Quit: GitException", customMessage: "the newest failure is kept");
        fitted.ShouldNotContain("view Sessions 0", customMessage: "the crash file's ordinary lines go first");
        fitted.ShouldContain("counts git status exit 128", customMessage: "non-zero counts outlast ordinary events");
        fitted.ShouldMatch(@"\n\(\d+ lines left out to fit the page address\)$");
    }

    [Fact]
    public async Task KeepEverythingWhenItFits()
    {
        log.Record(new ViewShown(ViewKind.Sessions));

        ProblemReport report = await Reports().BuildAsync();

        report.FitWithin(Title, ProblemReport.AddressLimit).ShouldBe(report.Text);
    }

    [Fact]
    public async Task DropOrdinaryEventsBeforeCountsAndCountsBeforeFailures()
    {
        log.Record(new ViewShown(ViewKind.Sessions));
        log.Record(new Failed(FailureSite.Load, null, ExceptionSummary.From(new GitException("x"))));
        log.Record(new ViewShown(ViewKind.Worktrees));
        counts.Add(ChildCommand.For(new ProcessRequest("git", ["status"], null)), 0, TimeSpan.FromMilliseconds(5));
        ProblemReport report = await Reports().BuildAsync();
        int withoutTwoEvents = ProblemReportAddress.Length(Title, report.Text) - Uri.EscapeDataString("2026-10-29T12:00:00.000Z view Sessions\n").Length - Uri.EscapeDataString("2026-10-29T12:00:00.000Z view Worktrees\n").Length + Uri.EscapeDataString("\n(2 lines left out to fit the page address)").Length;

        string fitted = report.FitWithin(Title, withoutTwoEvents);

        fitted.ShouldNotContain("view Sessions");
        fitted.ShouldNotContain("view Worktrees");
        fitted.ShouldContain("counts git status exit 0");
        fitted.ShouldContain("failed Load: GitException");
        fitted.ShouldEndWith("\n(2 lines left out to fit the page address)");
    }

    private ProblemReports Reports()
    {
        return new ProblemReports(log, counts, crashFile, Start, TestSettings.Current(@"C:\Users\me\source\repos"));
    }
}
