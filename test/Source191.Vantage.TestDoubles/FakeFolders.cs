using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>An in-memory folder tree for the read models and worktree actions, keyed case-insensitively like Windows paths.</summary>
public sealed class FakeFolders : IFolders
{
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Declares a folder and every folder above it.</summary>
    public FakeFolders WithDirectory(string path)
    {
        string? current = Normalise(path);
        while (current is not null && directories.Add(current))
        {
            current = Path.GetDirectoryName(current);
        }

        return this;
    }

    /// <summary>Declares a file (and its folders) with the given contents.</summary>
    public FakeFolders WithFile(string path, string contents = "")
    {
        string full = Normalise(path);
        WithDirectory(Path.GetDirectoryName(full)!);
        files[full] = contents;

        return this;
    }

    public bool DirectoryExists(string path)
    {
        return directories.Contains(Normalise(path));
    }

    public bool FileExists(string path)
    {
        return files.ContainsKey(Normalise(path));
    }

    public IEnumerable<string> Directories(string path)
    {
        string parent = Normalise(path);

        return directories.Where(directory => string.Equals(Path.GetDirectoryName(directory), parent, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> Files(string path)
    {
        string prefix = Normalise(path) + Path.DirectorySeparatorChar;

        return files.Keys.Where(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> FilesIn(string path)
    {
        string parent = Normalise(path);

        return files.Keys.Where(file => string.Equals(Path.GetDirectoryName(file), parent, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsEmpty(string path)
    {
        return !Files(path).Any() && !Directories(path).Any();
    }

    public IEnumerable<string> Lines(string file)
    {
        return files[Normalise(file)].Split('\n');
    }

    public void DeleteTree(string path)
    {
        string full = Normalise(path);
        string prefix = full + Path.DirectorySeparatorChar;
        directories.RemoveWhere(directory => string.Equals(directory, full, StringComparison.OrdinalIgnoreCase) || directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (string file in files.Keys.Where(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            files.Remove(file);
        }
    }

    private static string Normalise(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}