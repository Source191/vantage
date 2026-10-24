using Microsoft.Extensions.Logging;
namespace Source191.Vantage.Adapters.Diagnostics;

/// <summary>
/// A minimal append-to-file logger for diagnosing terminal problems in the field. Set VANTAGE_LOG to a file path
/// and both Vantage and Terminal.Gui's internals write there; nothing is logged otherwise.
/// </summary>
public sealed class FileLogger(string path, TimeProvider clock) : ILogger
{
    private readonly Lock gate = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string line = $"{clock.GetUtcNow():HH:mm:ss.fff} {logLevel,-11} {formatter(state, exception)}{(exception is null ? string.Empty : " " + exception)}";
        lock (gate)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}