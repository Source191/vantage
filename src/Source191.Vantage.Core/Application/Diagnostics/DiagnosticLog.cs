using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// The diagnostic log: the latest 500 events in memory for the life of the process, each also written as a line to
/// every sink (the <c>VANTAGE_LOG</c> file when it is set). Events arrive from the UI thread and from pool threads.
/// </summary>
public sealed class DiagnosticLog(TimeProvider clock, IReadOnlyList<IDiagnosticSink> sinks)
{
    public const int Capacity = 500;

    private readonly Lock gate = new();
    private readonly Queue<TimedEvent> events = new();

    public void Record(DiagnosticEvent diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        TimedEvent timed = new(clock.GetUtcNow(), diagnostic);
        lock (gate)
        {
            events.Enqueue(timed);
            if (events.Count > Capacity)
            {
                events.Dequeue();
            }

            Write([DiagnosticLine.Render(timed)]);
        }
    }

    public IReadOnlyList<TimedEvent> Snapshot()
    {
        lock (gate)
        {
            return [.. events];
        }
    }

    /// <summary>Writes the child program counts to the sinks without keeping them as events, as Vantage does when it exits.</summary>
    public void WriteCounts(ProcessCounts counts)
    {
        lock (gate)
        {
            Write(DiagnosticLine.Render(counts));
        }
    }

    private void Write(IReadOnlyList<string> lines)
    {
        foreach (IDiagnosticSink sink in sinks)
        {
            foreach (string line in lines)
            {
                sink.Write(line);
            }
        }
    }
}
