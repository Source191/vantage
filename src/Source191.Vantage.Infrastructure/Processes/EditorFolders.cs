namespace Source191.Vantage.Adapters.Processes;

/// <summary>The folders editors are installed under, and the PATH, so a test can point them at a temporary tree.</summary>
public sealed record EditorFolders(IReadOnlyList<string> PathFolders, string LocalAppData, string ProgramFiles, string ProgramFilesX86, string Windows)
{
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