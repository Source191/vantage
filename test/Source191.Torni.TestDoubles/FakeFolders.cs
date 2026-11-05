using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

/// <summary>An in-memory folder tree for the read models and worktree actions, keyed case-insensitively like Windows paths.</summary>
public sealed class FakeFolders : IFolders, IFolderDeleter
{
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> heldOpen = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>Makes <c>DeleteTree</c> of the folder throw an <see cref="IOException"/>, as a folder a program holds open does.</summary>
    public FakeFolders HeldOpen(string path)
    {
        heldOpen.Add(Normalise(path));

        return this;
    }

    public bool DirectoryExists(string path)
    {
        return directories.Contains(Normalise(path));
    }

    /// <summary>Blank text names no file, as File.Exists answers for it.</summary>
    public bool FileExists(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && files.ContainsKey(Normalise(path));
    }

    public IEnumerable<string> Directories(string path)
    {
        string parent = Declared(path);

        return directories.Where(directory => string.Equals(Path.GetDirectoryName(directory), parent, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> Files(string path)
    {
        string prefix = Declared(path) + Path.DirectorySeparatorChar;

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
        string full = Normalise(file);

        return files.TryGetValue(full, out string? contents)
            ? contents.Split('\n')
            : throw new FileNotFoundException($"Could not find file '{full}'.", full);
    }

    public void DeleteTree(string path)
    {
        RefuseRootsAndRelativePaths(path);
        string full = Normalise(path);
        if (heldOpen.Contains(full))
        {
            throw new IOException($"The process cannot access the file '{full}' because it is being used by another process.");
        }

        string prefix = full + Path.DirectorySeparatorChar;
        directories.RemoveWhere(directory => string.Equals(directory, full, StringComparison.OrdinalIgnoreCase) || directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (string file in files.Keys.Where(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            files.Remove(file);
        }
    }

    private static void RefuseRootsAndRelativePaths(string path)
    {
        if (!Path.IsPathFullyQualified(path) || IsRoot(path))
        {
            throw new IOException($"Torni will not delete {path}: it is a drive root, a share root or a relative path.");
        }
    }

    private static bool IsRoot(string path)
    {
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full) ?? full;

        return string.Equals(Path.TrimEndingDirectorySeparator(full), Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase);
    }

    private string Declared(string path)
    {
        string full = Normalise(path);

        return directories.Contains(full) ? full : throw new DirectoryNotFoundException($"Could not find a part of the path '{full}'.");
    }

    private static string Normalise(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}