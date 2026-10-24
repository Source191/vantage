using Microsoft.Extensions.Logging;

namespace Source191.Vantage.Adapters.Diagnostics;

/// <summary>The handful of lines Vantage itself writes to the diagnostic log around a terminal hand-off.</summary>
public static partial class HandoffLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Vantage started, pid {Pid}, Terminal.Gui logging on")]
    public static partial void Started(this ILogger log, int pid);

    /// <summary>Takes the effect's description rather than the effect, so the adapter knows nothing of the views.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Handing the terminal to {Effect}")]
    public static partial void HandingOver(this ILogger log, string effect);

    [LoggerMessage(Level = LogLevel.Information, Message = "Child returned; resetting terminal modes and discarding pending input")]
    public static partial void ChildReturned(this ILogger log);
}