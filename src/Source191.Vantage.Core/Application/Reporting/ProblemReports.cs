using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Reporting;

/// <summary>
/// Builds a problem report from what Vantage already records and nothing else: the start facts, the shape of the
/// settings (the editor preset, whether update checks are on, how many roots), the last crash, the diagnostic log and
/// the child program counts. No path, name or typed text is read here.
/// </summary>
public sealed class ProblemReports(DiagnosticLog log, ProcessCounts counts, ICrashFile crashFile, Started start, CurrentSettings settings)
{
    private const int CrashBand = 0;
    private const int OrdinaryBand = 100_000;
    private const int ExitZeroBand = 200_000;
    private const int FailureBand = 300_000;
    private const string CrashMarker = " failed Crash: ";

    /// <summary>The title a report takes when its title box is left blank.</summary>
    public ReportTitle DefaultTitle => ReportTitle.From(Wording.DefaultReportTitle(start.Vantage));

    public async Task<ProblemReport> BuildAsync()
    {
        string? crash = await crashFile.ReadAsync();
        List<ReportLine> lines = [Kept("Environment"), Kept(DiagnosticLine.Environment(start)), Kept(SettingsLine()), Kept(string.Empty)];
        if (crash is not null)
        {
            lines.AddRange([Kept("Last crash"), .. CrashLines(crash), Kept(string.Empty)]);
        }

        lines.AddRange([Kept("Recent events"), .. EventLines(), Kept(string.Empty), Kept("Child program counts"), .. CountLines()]);

        return new ProblemReport(lines, crash is not null);
    }

    private string SettingsLine()
    {
        Domain.Settings current = settings.Value;
        int roots = current.RepositoryRoots.Count;

        return $"Settings: editor {current.Editor.Preset}, update checks {(current.CheckForUpdates ? "on" : "off")}, {roots} repository {(roots == 1 ? "root" : "roots")}";
    }

    private static IEnumerable<ReportLine> CrashLines(string crash) => crash.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select((line, index) => new ReportLine(line, line.Contains(CrashMarker, StringComparison.Ordinal) ? ReportLine.Never : CrashBand + index));

    private IEnumerable<ReportLine> EventLines() => log.Snapshot()
        .Select((timed, index) => new ReportLine(DiagnosticLine.Render(timed), (timed.Event is Failed or ProcessFailedToStart ? FailureBand : OrdinaryBand) + index));

    private IEnumerable<ReportLine> CountLines() => counts.Snapshot()
        .Zip(DiagnosticLine.Render(counts))
        .Select((pair, index) => new ReportLine(pair.Second, (pair.First.ExitCode == 0 ? ExitZeroBand : FailureBand + 50_000) + index));

    private static ReportLine Kept(string text) => new(text, ReportLine.Never);
}
