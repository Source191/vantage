namespace Source191.Vantage.Adapters.Files;

/// <summary>
/// Deletes a folder tree the way git leaves them: object and pack files are read-only, which Directory.Delete refuses,
/// so every file is made writable first. Junctions and symbolic links (pnpm's node_modules is full of them) are
/// removed as links and never entered, so nothing outside the folder is changed.
/// </summary>
public static class FolderDeletion
{
    public static void DeleteTree(string folder)
    {
        RefuseRootsAndRelativePaths(folder);
        if (!Directory.Exists(folder))
        {
            return;
        }

        if (File.GetAttributes(folder).HasFlag(FileAttributes.ReparsePoint))
        {
            Directory.Delete(folder);
            return;
        }

        PrepareContents(folder);
        Directory.Delete(folder, recursive: true);
    }

    // Checked before anything on disc, so even a root that does not exist is refused rather than quietly ignored. The
    // message is shown in the footer, so it is written for the user rather than built by a throw helper.
    private static void RefuseRootsAndRelativePaths(string folder)
    {
        if (!Path.IsPathFullyQualified(folder) || IsRoot(folder))
        {
            throw new IOException($"Vantage will not delete {folder}: it is a drive root, a share root or a relative path.");
        }
    }

    private static bool IsRoot(string folder)
    {
        string full = Path.GetFullPath(folder);
        // A fully qualified path always has a root; if one ever did not, treating it as a root refuses it.
        string root = Path.GetPathRoot(full) ?? full;

        return string.Equals(Path.TrimEndingDirectorySeparator(full), Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase);
    }

    private static void PrepareContents(string folder)
    {
        foreach (string entry in Directory.EnumerateFileSystemEntries(folder))
        {
            Prepare(entry);
        }
    }

    private static void Prepare(string entry)
    {
        FileAttributes attributes = File.GetAttributes(entry);
        bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            RemoveLink(entry, isDirectory);
            return;
        }

        if (isDirectory)
        {
            PrepareContents(entry);
            return;
        }

        File.SetAttributes(entry, FileAttributes.Normal);
    }

    private static void RemoveLink(string link, bool isDirectory)
    {
        if (isDirectory)
        {
            Directory.Delete(link);
            return;
        }

        File.Delete(link);
    }
}