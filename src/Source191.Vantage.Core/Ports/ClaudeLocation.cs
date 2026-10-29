namespace Source191.Vantage.Ports;

/// <summary>
/// Where Claude Code was found: an executable to run, only a batch file (which Vantage never runs, because cmd.exe
/// would read session names and prompts as commands), or nothing. <c>Wording</c> says the last two.
/// </summary>
public abstract record ClaudeLocation
{
    public sealed record Found(string Path) : ClaudeLocation;

    public sealed record BatchFileOnly(string Path) : ClaudeLocation;

    public sealed record Missing : ClaudeLocation;
}
