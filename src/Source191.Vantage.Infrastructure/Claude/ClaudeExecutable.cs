using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// Finds the Claude Code executable: the native user-scope install first, then a claude.exe in a folder on the PATH.
/// A claude.cmd or claude.bat alone (the npm shim) is reported, never run: cmd.exe would read session names and
/// prompts as commands.
/// </summary>
public static class ClaudeExecutable
{
    private static readonly string[] BatchFiles = ["claude.cmd", "claude.bat"];

    public static ClaudeLocation Locate() => Locate(new Folders(), PathFolders.ForCurrentUser(), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public static ClaudeLocation Locate(IFolders folders, IReadOnlyList<string> pathFolders, string home)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(pathFolders);
        string native = Path.Combine(home, ".local", "bin", "claude.exe");
        if (folders.FileExists(native))
        {
            return new ClaudeLocation.Found(native);
        }

        if (new ProgramLocator(folders, pathFolders).Locate("claude") is ProgramLocation.Found executable)
        {
            return new ClaudeLocation.Found(executable.Path);
        }

        string? batchFile = pathFolders
            .SelectMany(folder => BatchFiles.Select(name => Path.Combine(folder, name)))
            .FirstOrDefault(folders.FileExists);

        return batchFile is null ? new ClaudeLocation.Missing() : new ClaudeLocation.BatchFileOnly(batchFile);
    }
}