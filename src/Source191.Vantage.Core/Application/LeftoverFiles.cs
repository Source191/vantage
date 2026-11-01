using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using IOPath = System.IO.Path;

namespace Source191.Vantage.Application;

/// <summary>
/// Files git no longer tracks in a leftover folder, so would be lost with it; the .git pointer file itself does not
/// count. Counting stops at the Domain limit so a leftover with a dependencies tree inside does not slow every scan.
/// The scan and the removals count the same way, the removals at the moment a key is pressed.
/// </summary>
public static class LeftoverFiles
{
    public static int Count(IFolders folders, FolderPath folder)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(folder);
        if (!folders.DirectoryExists(folder.Value))
        {
            return 0;
        }

        return folders.Files(folder.Value)
            .Where(file => !string.Equals(IOPath.GetFileName(file), ".git", StringComparison.OrdinalIgnoreCase))
            .Take(WorktreeRemovalFacts.LeftoverFileLimit)
            .Count();
    }
}