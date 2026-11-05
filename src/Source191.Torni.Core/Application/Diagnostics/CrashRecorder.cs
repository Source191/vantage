using Source191.Torni.Ports;

namespace Source191.Torni.Application.Diagnostics;

/// <summary>
/// Leaves the diagnostic log behind when an exception ends the console: the events, the counts and the crash
/// itself go to the crash file, and the exception carries on so the command line reports it. A normal return
/// writes the counts to the log's sinks instead. Nothing here may hide the original exception.
/// </summary>
public sealed class CrashRecorder(DiagnosticLog log, PerformanceCounts counts, ICrashFile crashFile, TimeProvider clock)
{
    public async Task<int> GuardAsync(Func<Task<int>> action)
    {
        try
        {
            int exitCode = await action();
            log.WriteCounts(counts);

            return exitCode;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Records a crash the guard cannot see, such as an exception on a thread of its own.</summary>
    public void Record(Exception exception)
    {
        log.Record(new Failed(FailureSite.Crash, null, ExceptionSummary.From(exception)));
        string text = string.Join('\n', [.. log.Snapshot().Select(DiagnosticLine.Render), .. DiagnosticLine.Render(counts, clock.GetUtcNow())]) + "\n";
        try
        {
            crashFile.Write(text);
        }
        catch (Exception unwritten) when (unwritten is IOException or UnauthorizedAccessException)
        {
            // The crash file is a courtesy; the crash itself must still be reported.
        }
    }

    public void RecordUnobserved(Exception exception) => log.Record(new Failed(FailureSite.BackgroundTask, null, ExceptionSummary.From(exception)));
}