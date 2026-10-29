using Microsoft.Extensions.Logging;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Terminal.Gui's logger, assigned to <c>Terminal.Gui.App.Logging.Logger</c>. Its messages can quote input it failed to
/// parse, which can be typed or pasted text, so only the level and an exception's summary are recorded, for warnings
/// and above; the message and its state are never read.
/// </summary>
public sealed class TerminalGuiBridge(DiagnosticLog log) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel is LogLevel.Warning or LogLevel.Error or LogLevel.Critical;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        log.Record(new TerminalGuiReported(Level(logLevel), exception is null ? null : ExceptionSummary.From(exception)));
    }

    private static DiagnosticLevel Level(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Error => DiagnosticLevel.Error,
        LogLevel.Critical => DiagnosticLevel.Critical,
        _ => DiagnosticLevel.Warning,
    };
}