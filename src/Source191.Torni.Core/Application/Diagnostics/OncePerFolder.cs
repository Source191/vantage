using Source191.Torni.Domain;
using System.Collections.Concurrent;

namespace Source191.Torni.Application.Diagnostics;

/// <summary>
/// Records a failure the first time a folder fails in this process run and answers the fallback every time, so a
/// repository that stays unreadable adds one event to the log rather than one per scan. The folder is a key in memory
/// and never reaches the event.
/// </summary>
public sealed class OncePerFolder(DiagnosticLog log, FailureSite site, CodeName what)
{
    private readonly ConcurrentDictionary<FolderPath, byte> failed = new();

    public async Task<T> RunAsync<T>(FolderPath folder, Func<Task<T>> action, Func<Exception, bool> handles, Func<Exception, T> onFailure)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (handles(exception))
        {
            Record(folder, exception);

            return onFailure(exception);
        }
    }

    private void Record(FolderPath folder, Exception exception)
    {
        if (failed.TryAdd(folder, 0))
        {
            log.Record(new Failed(site, what, ExceptionSummary.From(exception)));
        }
    }
}