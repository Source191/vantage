using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Finds a program's executable in the folders of the PATH, in order, so a child is started by its full path. Given a
/// bare name, Windows would search the folder Vantage was started from before the PATH, and a cloned repository can
/// hold a git.exe. Only an .exe is answered: a .cmd or .bat would run through cmd.exe, which reads arguments as commands.
/// </summary>
public sealed class ProgramLocator(IFolders folders, IReadOnlyList<string> pathFolders)
{
    private const string Executable = ".exe";

    public static ProgramLocator ForCurrentUser() => new(new Folders(), PathFolders.ForCurrentUser());

    /// <summary>The full path of the program's .exe, or missing when no PATH folder holds one.</summary>
    public ProgramLocation Locate(string program)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(program);
        string fileName = Path.HasExtension(program) ? program : program + Executable;
        string? found = fileName.EndsWith(Executable, StringComparison.OrdinalIgnoreCase)
            ? pathFolders.Select(folder => Path.Combine(folder, fileName)).FirstOrDefault(folders.FileExists)
            : null;

        return found is null ? new ProgramLocation.Missing() : new ProgramLocation.Found(found);
    }
}