namespace Source191.Torni.Domain;

/// <summary>
/// What a child of Torni must not inherit. A Torni started from inside a Claude Code session carries that
/// session's markers (CLAUDECODE, CLAUDE_CODE_*, CLAUDE_PID), its NO_COLOR and its GIT_TERMINAL_PROMPT, and Windows
/// Terminal hands the caller's environment to a new tab, so without this every tab and session Torni opened ran
/// colourless, thought it was nested and had git refuse to ask for credentials. CLAUDE_CONFIG_DIR stays: it is how a
/// second account is chosen.
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