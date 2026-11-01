namespace Source191.Vantage.Ports;

/// <summary>
/// What a resume under the supervisor did. Continued: the CLI succeeded without printing a note, and Message is its
/// output. Failed: the CLI failed, or printed a note such as that a copy was started; Message is its error output or
/// that note.
/// </summary>
public abstract record ResumeResult(string Message)
{
    public sealed record Continued(string Message) : ResumeResult(Message);

    public sealed record Failed(string Message) : ResumeResult(Message);
}
