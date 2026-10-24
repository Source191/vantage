namespace Source191.Vantage.Ports;

/// <summary>Claude Code's transcript files, one per session, written only once a session has been sent a message.</summary>
public interface ITranscripts
{
    bool HasMessages(string sessionId);
}