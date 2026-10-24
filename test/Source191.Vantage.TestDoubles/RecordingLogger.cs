using Microsoft.Extensions.Logging;

namespace Source191.Vantage.TestDoubles;

/// <summary>Keeps every formatted log line so a test can read what was written.</summary>
public sealed class RecordingLogger : ILogger
{
    private readonly List<string> lines = [];

    public IReadOnlyList<string> Lines => lines;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lines.Add(formatter(state, exception));
    }
}