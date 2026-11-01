using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>What starting a background session answered, with the CLI's own message in every case.</summary>
public abstract record StartResult(string Message)
{
    /// <summary>The CLI backgrounded the session under this job id.</summary>
    public sealed record Started(JobId JobId, string Message) : StartResult(Message);

    /// <summary>The folder's repository has not been trusted interactively yet.</summary>
    public sealed record WorkspaceNotTrusted(string Message) : StartResult(Message);

    public sealed record Failed(string Message) : StartResult(Message);
}