using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>A stand-in for Claude Code's transcript files: the sessions that have been sent at least one message.</summary>
public sealed class FakeTranscripts : ITranscripts
{
    private readonly HashSet<string> withMessages = new(StringComparer.OrdinalIgnoreCase);

    public FakeTranscripts WithMessages(string sessionId)
    {
        withMessages.Add(sessionId);
        return this;
    }

    public bool HasMessages(string sessionId)
    {
        return withMessages.Contains(sessionId);
    }
}