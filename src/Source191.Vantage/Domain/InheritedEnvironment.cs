namespace Source191.Vantage.Domain;

/// <summary>
/// What a child of Vantage must not inherit. A Vantage started from inside a Claude Code session carries that
/// session's markers (CLAUDECODE, CLAUDE_CODE_*, CLAUDE_PID) and its NO_COLOR, and Windows Terminal hands the
/// caller's environment to a new tab, so without this every tab and session Vantage opened ran colourless and
/// thought it was nested. CLAUDE_CONFIG_DIR stays: it is how a second account is chosen.
/// </summary>
public static class InheritedEnvironment
{
    private static readonly string[] Exact = ["NO_COLOR", "CLAUDECODE", "CLAUDE_PID", "GIT_TERMINAL_PROMPT"];

    public static bool ShouldDrop(string name)
    {
        return Exact.Contains(name, StringComparer.OrdinalIgnoreCase)
            || name.StartsWith("CLAUDE_CODE_", StringComparison.OrdinalIgnoreCase);
    }
}