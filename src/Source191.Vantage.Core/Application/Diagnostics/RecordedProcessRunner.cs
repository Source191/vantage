using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Counts every child program Vantage runs and records a line for one that is slow or cannot be started. It wraps the
/// one process runner, so every adapter is covered. An interactive run is a hand-off and is never called slow.
/// </summary>
public sealed class RecordedProcessRunner(IProcessRunner inner, DiagnosticLog log, ProcessCounts counts, TimeProvider clock) : IProcessRunner
{
    private static readonly TimeSpan Slow = TimeSpan.FromSeconds(2);

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ChildCommand command = ChildCommand.For(request);
        long started = clock.GetTimestamp();
        ProcessResult result = await Started(command, () => inner.RunAsync(request, cancellationToken));
        TimeSpan duration = clock.GetElapsedTime(started);
        counts.Add(command, result.ExitCode, duration);
        if (duration > Slow)
        {
            log.Record(new ProcessSlow(command, result.ExitCode, duration));
        }

        return result;
    }

    public async Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ChildCommand command = ChildCommand.For(request);
        long started = clock.GetTimestamp();
        int exitCode = await Started(command, () => inner.RunInteractiveAsync(request, cancellationToken));
        counts.Add(command, exitCode, clock.GetElapsedTime(started));

        return exitCode;
    }

    private async Task<T> Started<T>(ChildCommand command, Func<Task<T>> run)
    {
        try
        {
            return await run();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Record(new ProcessFailedToStart(command, ExceptionSummary.From(exception)));
            throw;
        }
    }
}
