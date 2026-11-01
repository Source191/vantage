namespace Source191.Vantage.Adapters.Processes;

/// <summary>The folders editors are installed under, and the PATH, so a test can point them at a temporary tree.</summary>
public sealed record EditorFolders(IReadOnlyList<string> PathFolders, string LocalAppData, string ProgramFiles, string ProgramFilesX86, string Windows)
{
    /// <summary>
    /// The user's own Programs folder, the default of
    /// <see href="https://learn.microsoft.com/windows/win32/shell/knownfolderid">FOLDERID_UserProgramFiles</see>, which
    /// <see cref="Environment.SpecialFolder"/> does not name: VS Code's user install and JetBrains Toolbox 2.0 and later put
    /// their programs there.
    /// </summary>
    public string UserPrograms => Path.Combine(LocalAppData, "Programs");

    public static EditorFolders ForCurrentUser()
    {
        return new EditorFolders(
            Processes.PathFolders.ForCurrentUser(),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows));
    }
}