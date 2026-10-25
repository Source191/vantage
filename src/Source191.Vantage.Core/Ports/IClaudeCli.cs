using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

public enum StartOutcome
{
    Started,
    WorkspaceNotTrusted,
    Failed,
}

public sealed record StartResult(StartOutcome Outcome, string? JobId, string Message);

public sealed record ResumeResult(bool Continued, string Message);

/// <summary>
/// The documented Claude Code CLI surface Vantage depends on. Nothing else about Claude Code is touched.
/// </summary>
public interface IClaudeCli
{
    Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken);

    Task<StartResult> StartBackgroundAsync(string directory, SessionName name, string? openingPrompt, CancellationToken cancellationToken);

    Task<int> AttachAsync(string jobId, CancellationToken cancellationToken);

    Task StopAsync(string jobId, CancellationToken cancellationToken);

    Task RemoveAsync(string jobId, CancellationToken cancellationToken);

    /// <summary>Continues a saved conversation under the supervisor; the display name is kept when one is given.</summary>
    Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, SessionName? name, CancellationToken cancellationToken);

    Task<int> OpenAgentViewAsync(CancellationToken cancellationToken);

    /// <summary>The Claude Code version, such as "2.1.310"; null when the CLI cannot be run, is cancelled or prints none.</summary>
    Task<string?> VersionAsync(CancellationToken cancellationToken);
}