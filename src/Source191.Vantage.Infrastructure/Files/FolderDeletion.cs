namespace Source191.Vantage.Adapters.Files;

/// <summary>
/// Deletes a folder tree the way git leaves them: object and pack files are read-only, which Directory.Delete refuses,
/// so every file is made writable first.
/// </summary>
public static class FolderDeletion
{
    public static void DeleteTree(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(folder, recursive: true);
    }
}