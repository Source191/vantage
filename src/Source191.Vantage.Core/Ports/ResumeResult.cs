namespace Source191.Vantage.Ports;

/// <summary>
/// What a resume under the supervisor did. Continued is true when the CLI succeeded without printing a note; Message is
/// that note, the CLI's error output when it failed, or its output otherwise.
/// </summary>
public sealed record ResumeResult(bool Continued, string Message);