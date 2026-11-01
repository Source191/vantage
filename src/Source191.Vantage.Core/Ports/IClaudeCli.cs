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

public sealed record ResumeResult(bool Continued, string Message);

/// <summary>
/// The documented Claude Code CLI surface Vantage depends on. Nothing else about Claude Code is touched.
/// </summary>
public interface IClaudeCli
{
    Task<SessionListing> ListSessionsAsync(CancellationToken cancellationToken);

    Task<StartResult> StartBackgroundAsync(string directory, SessionName name, OpeningPrompt openingPrompt, CancellationToken cancellationToken);

    Task<int> AttachAsync(string jobId, CancellationToken cancellationToken);

    Task StopAsync(string jobId, CancellationToken cancellationToken);

    Task RemoveAsync(string jobId, CancellationToken cancellationToken);

    /// <summary>Continues a saved conversation under the supervisor under the display name given.</summary>
    Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, SessionName name, CancellationToken cancellationToken);

    /// <summary>Continues a saved conversation under the supervisor with no display name, for a listed name that is not a session name.</summary>
    Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, CancellationToken cancellationToken);

    /// <summary>The Claude Code version, such as "2.1.310", as printed; NotRead when the CLI cannot be run, is cancelled or prints none.</summary>
    Task<ClaudeVersionReport> VersionAsync(CancellationToken cancellationToken);
}