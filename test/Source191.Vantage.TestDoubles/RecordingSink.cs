using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Keeps every diagnostic line written to it, in place of the <c>VANTAGE_LOG</c> file.</summary>
public sealed class RecordingSink : IDiagnosticSink
{
    private readonly Lock gate = new();
    private readonly List<string> lines = [];

    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (gate)
            {
                return [.. lines];
            }
        }
    }

    public void Write(string line)
    {
        lock (gate)
        {
            lines.Add(line);
        }
    }
}
