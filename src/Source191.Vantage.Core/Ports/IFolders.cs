namespace Source191.Vantage.Ports;

/// <summary>
/// The little the read models and the worktree actions need from the file system: existence, the folders and files
/// under a path, whether a folder is empty, the lines of a small text file such as a .git pointer, and deleting a
/// leftover tree. Paths in are full paths; paths out are full paths too. Implemented over System.IO at the boundary
/// and faked in tests.
/// </summary>
public interface IFolders
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>The folders directly under <paramref name="path"/>, in no particular order.</summary>
    IEnumerable<string> Directories(string path);

    /// <summary>Every file under <paramref name="path"/> at any depth, enumerated lazily so a caller can stop early.</summary>
    IEnumerable<string> Files(string path);

    /// <summary>The files directly in <paramref name="path"/>, none of the folders beneath it; nothing when the folder is missing.</summary>
    IEnumerable<string> FilesIn(string path);

    /// <summary>True when the folder holds neither files nor folders.</summary>
    bool IsEmpty(string path);

    IEnumerable<string> Lines(string file);

    /// <summary>Deletes a folder tree, including read-only files such as git objects. A missing folder is not an error.</summary>
    void DeleteTree(string path);
}