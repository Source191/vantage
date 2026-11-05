using Source191.Torni.Application.Views;

namespace Source191.Torni.Application.Diagnostics;

/// <summary>
/// The one place a screen turns a failure into what it shows (a load failure, a failed status message): the failure is recorded
/// with its summary on the way, and the screen gets the answer it built from the exception.
/// </summary>
public static class Attempt
{
    public static Task<T> RunAsync<T>(DiagnosticLog log, FailureSite site, CodeName what, Func<Task<T>> action, Func<Exception, T> onFailure) => RunAsync(log, site, what, action, _ => true, onFailure);

    /// <summary>As above, for the failures handles accepts; any other exception propagates unrecorded, as a cancellation should.</summary>
    public static async Task<T> RunAsync<T>(DiagnosticLog log, FailureSite site, CodeName what, Func<Task<T>> action, Func<Exception, bool> handles, Func<Exception, T> onFailure)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (handles(exception))
        {
            log.Record(new Failed(site, what, ExceptionSummary.From(exception)));

            return onFailure(exception);
        }
    }

    /// <summary>What a background job was doing, from the status message it showed meanwhile: the effect it waits for, or the message's own kind.</summary>
    public static CodeName What(StatusMessage working) => working is StatusMessage.Working waiting ? CodeName.For(waiting.Effect.GetType()) : CodeName.For(working.GetType());
}