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

    /// <summary>The Claude Code version, such as "2.1.310"; null when the CLI cannot be run, is cancelled or prints none.</summary>
    Task<string?> VersionAsync(CancellationToken cancellationToken);

    /// <summary>A background job's whole screen history as <c>claude logs</c> prints it (see <see cref="SessionScreen"/>); null when the command fails or cannot be run. Never recorded.</summary>
    Task<string?> LogsAsync(string jobId, CancellationToken cancellationToken);
}