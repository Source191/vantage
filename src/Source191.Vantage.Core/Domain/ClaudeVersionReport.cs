namespace Source191.Vantage.Domain;

/// <summary>
/// What <c>claude --version</c> gave: nothing read (the CLI could not be run, was cancelled or printed no version), or
/// the leading dotted number it printed, as text, which <see cref="ClaudeCodeSupport.Check"/> judges.
/// </summary>
public abstract record ClaudeVersionReport
{
    public sealed record NotRead : ClaudeVersionReport;

    public sealed record Printed(string Text) : ClaudeVersionReport;
}