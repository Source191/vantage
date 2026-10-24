namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// Finds the Claude Code executable: the native user-scope install first, then anything named claude on the PATH.
/// </summary>
public static class ClaudeExecutable
{
    public static string? Locate()
    {
        string native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");
        if (File.Exists(native))
        {
            return native;
        }

        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (string candidate in new[] { "claude.exe", "claude.cmd" })
            {
                string full = Path.Combine(folder.Trim(), candidate);
                if (File.Exists(full))
                {
                    return full;
                }
            }
        }

        return null;
    }
}