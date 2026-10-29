using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class ProblemReportHandlersShould
{
    private static readonly Started Start = new(new ToolVersion("0.17.0-preview.6"), new Version(2, 1, 312), new Version(10, 0, 26100), new Version(10, 0, 14), 120, 36);
    private static readonly ReportTitle Title = ReportTitle.From("Attach froze after Ctrl+Z");

    private readonly FakeCrashFile crashFile = new();
    private readonly FakeDesktop desktop = new();
    private readonly FakeClipboard clipboard = new();
    private readonly DiagnosticLog log = new(new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)), []);

    [Fact]
    public async Task OpenTheFittedAddressAndRemoveTheCrash()
    {
        crashFile.Write("2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException\n");
        ProblemReport report = await Report();

        Outcome outcome = await new OpenProblemReportHandler(desktop, crashFile).HandleAsync(new OpenProblemReport(Title, report), CancellationToken.None);

        outcome.ShouldBe(new ProblemReportOutcome.Opened());
        desktop.Opened.ShouldBe([ProblemReportAddress.For(Title, report.FitWithin(Title, ProblemReport.AddressLimit)).AbsoluteUri]);
        crashFile.Removed.ShouldBeTrue();
    }

    [Fact]
    public async Task LeaveTheCrashFileAloneWithoutACrash()
    {
        ProblemReport report = await Report();

        await new OpenProblemReportHandler(desktop, crashFile).HandleAsync(new OpenProblemReport(Title, report), CancellationToken.None);

        crashFile.Removed.ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveTheCrashWhenTheBrowserFails()
    {
        crashFile.Write("2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException\n");
        desktop.OpenFailure = new InvalidOperationException("No application is associated with the address.");
        ProblemReport report = await Report();

        Outcome outcome = await new OpenProblemReportHandler(desktop, crashFile).HandleAsync(new OpenProblemReport(Title, report), CancellationToken.None);

        outcome.ShouldBe(new ProblemReportOutcome.NotOpened("No application is associated with the address."));
        crashFile.Removed.ShouldBeFalse();
    }

    [Fact]
    public async Task CopyTheTitleAndTheFullText()
    {
        crashFile.Write("2026-10-28T09:00:01.000Z failed Crash: InvalidOperationException\n");
        ProblemReport report = await Report();

        Outcome outcome = await new CopyProblemReportHandler(clipboard).HandleAsync(new CopyProblemReport(Title, report), CancellationToken.None);

        outcome.ShouldBe(new ProblemReportOutcome.Copied());
        clipboard.Text.ShouldBe($"{Title.Value}\n\n{report.Text}");
        crashFile.Removed.ShouldBeFalse();
    }

    [Fact]
    public async Task SayWhenTheClipboardIsBusy()
    {
        clipboard.Busy = true;

        Outcome outcome = await new CopyProblemReportHandler(clipboard).HandleAsync(new CopyProblemReport(Title, await Report()), CancellationToken.None);

        outcome.ShouldBe(new ClipboardBusy());
    }

    private Task<ProblemReport> Report()
    {
        return new ProblemReports(log, new ProcessCounts(), crashFile, Start, TestSettings.Current(@"C:\Users\me\source\repos")).BuildAsync();
    }
}
