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
        string at = timed.At.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        return $"{at} {Body(timed.Event)}";
    }

    public static IReadOnlyList<string> Render(ProcessCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return [.. counts.Snapshot().Select(count => $"counts {count.Command} exit {count.ExitCode}: {Runs(count.Runs)}, longest {Milliseconds(count.Longest)}")];
    }

    private static string Body(DiagnosticEvent diagnostic) => diagnostic switch
    {
        Started started => $"started Vantage {started.Vantage.Text}, Claude Code {started.ClaudeCode?.ToString() ?? "unknown"}, Windows {started.Windows}, .NET {started.Runtime}, terminal {started.Columns}x{started.Rows}",
        ViewShown view => $"view {view.View}",
        DialogShown dialog => $"dialog {dialog.Dialog}",
        EffectRan ran => $"effect {ran.Effect} -> {ran.Outcome} in {Milliseconds(ran.Duration)}",
        HandingOver handing => $"hand-off {handing.Effect}",
        ChildReturned => "hand-off returned",
        ProcessSlow slow => $"slow {slow.Command} exit {slow.ExitCode} in {Milliseconds(slow.Duration)}",
        ProcessFailedToStart failed => $"not started {failed.Command}: {failed.Failure}",
        Failed failed => failed.What is null ? $"failed {failed.Site}: {failed.Failure}" : $"failed {failed.Site} {failed.What}: {failed.Failure}",
        TerminalGuiReported reported => reported.Failure is null ? $"terminal.gui {reported.Level}" : $"terminal.gui {reported.Level}: {reported.Failure}",
        _ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.GetType().Name, "A diagnostic event has no line."),
    };

    private static string Milliseconds(TimeSpan duration) => $"{(long)duration.TotalMilliseconds} ms";

    private static string Runs(int runs) => runs == 1 ? "1 run" : $"{runs} runs";
}
