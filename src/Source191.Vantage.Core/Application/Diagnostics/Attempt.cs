using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// The one place a screen turns a failure into what it shows (a load failure, a failed footer): the failure is recorded
/// with its summary on the way, and the screen gets the answer it built from the exception, as it did before.
/// </summary>
public static class Attempt
{
    public static async Task<T> RunAsync<T>(DiagnosticLog log, FailureSite site, CodeName? what, Func<Task<T>> action, Func<Exception, T> onFailure)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(onFailure);
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            log.Record(new Failed(site, what, ExceptionSummary.From(exception)));

            return onFailure(exception);
        }
    }

    /// <summary>What a background job was doing, from the footer it showed meanwhile: the effect it waits for, or the footer's own kind.</summary>
    public static CodeName What(Footer working) => working is Footer.Working waiting ? CodeName.For(waiting.Effect.GetType()) : CodeName.For(working.GetType());
}