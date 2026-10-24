using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Files;

/// <summary>The file system as <see cref="IFolders"/> sees it.</summary>
public sealed class Folders : IFolders
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> Directories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> Files(string path) => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);

    public bool IsEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();

    public IEnumerable<string> Lines(string file) => File.ReadLines(file);

    public void DeleteTree(string path) => FolderDeletion.DeleteTree(path);
}