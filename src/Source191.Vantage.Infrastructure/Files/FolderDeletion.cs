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