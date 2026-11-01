using System.Globalization;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// The one text form of a diagnostic record, shared by the in-memory log, the <c>VANTAGE_LOG</c> file and the crash
/// file. These lines are a maintainer's format rather than interface text, so they live here and not in Wording.
/// </summary>
public static class DiagnosticLine
{
    public static string Render(TimedEvent timed)
    {
        ArgumentNullException.ThrowIfNull(timed);

        return $"{Stamp(timed.At)} {Body(timed.Event)}";
    }

    /// <summary>The child program counts, one line per program, subcommand and exit code, each with the time it was written.</summary>
    public static IReadOnlyList<string> Render(ProcessCounts counts, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return [.. counts.Snapshot().Select(count => $"{Stamp(at)} counts {count.Command} exit {count.ExitCode}: {Figures(count.Durations, "run", "runs")}")];
    }

    /// <summary>Every count, each line with the time it was written: the child programs, each refresh part that has run, and the UI thread's stalls even with none, so a file shows the heartbeat ran.</summary>
    public static IReadOnlyList<string> Render(PerformanceCounts counts, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(counts);
        IEnumerable<string> refreshes = counts.Refreshes.Snapshot().Select(count => $"{Stamp(at)} refreshes {Name(count.Part)}: {Figures(count.Durations, "run", "runs")}");

        string stalls = $"{Stamp(at)} stalls ui thread: {Figures(counts.Stalls.Figures(), "stall", "stalls")}";

        return [.. Render(counts.Processes, at), .. refreshes, stalls];
    }

    /// <summary>The start facts without the time, as a problem report's environment line shows them.</summary>
    public static string Environment(Started started)
    {
        ArgumentNullException.ThrowIfNull(started);

        return $"Vantage {started.Vantage.Text}, Claude Code {started.ClaudeCode?.ToString() ?? "unknown"}, Windows {started.Windows}, .NET {started.Runtime}, terminal {started.Columns}x{started.Rows}";
    }

    private static string Body(DiagnosticEvent diagnostic) => diagnostic switch
    {
        Started started => $"started {Environment(started)}",
        ViewShown view => $"view {view.View}",
        DialogShown dialog => $"dialog {dialog.Dialog}",
        EffectRan ran => $"effect {ran.Effect} -> {ran.Outcome} in {Milliseconds(ran.Duration)}",

        ProcessSlow slow => $"slow {slow.Command} exit {slow.ExitCode} in {Milliseconds(slow.Duration)}",
        RefreshSlow slow => $"slow refresh {Name(slow.Part)} in {Milliseconds(slow.Duration)}",
        UiThreadStalled stalled => $"stalled ui thread for {Milliseconds(stalled.Lateness)}",
        ProcessFailedToStart failed => $"not started {failed.Command}: {failed.Failure}",
        Failed failed => failed.What is null ? $"failed {failed.Site}: {failed.Failure}" : $"failed {failed.Site} {failed.What}: {failed.Failure}",
        TerminalGuiReported reported => reported.Failure is null ? $"terminal.gui {reported.Level}" : $"terminal.gui {reported.Level}: {reported.Failure}",
        _ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.GetType().Name, "A diagnostic event has no line."),
    };

    private static string Name(RefreshPart part) => part switch
    {
        RefreshPart.SessionsListing => "sessions listing",
        RefreshPart.SessionsEnrichment => "sessions enrichment",
        RefreshPart.WorktreesScan => "worktrees scan",
        RefreshPart.PullRequestRefresh => "pull-request refresh",
    };

    private static string Stamp(DateTimeOffset at) => at.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    /// <summary>The number of runs, then the typical and longest durations when there were any.</summary>
    private static string Figures(DurationFigures figures, string one, string many)
    {
        string runs = figures.Runs == 1 ? $"1 {one}" : $"{figures.Runs.ToString(CultureInfo.InvariantCulture)} {many}";

        return figures.Runs == 0
            ? runs
            : $"{runs}, median {Milliseconds(figures.Median)}, p95 {Milliseconds(figures.NinetyFifth)}, longest {Milliseconds(figures.Longest)}";
    }

    private static string Milliseconds(TimeSpan duration) => $"{((long)duration.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)} ms";
}