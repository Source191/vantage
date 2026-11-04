namespace Source191.Torni.Ports;

/// <summary>
/// Deletes a leftover folder tree. Only <c>WorktreeRemover</c> holds it (<c>LayeringShould</c>), so a folder is deleted
/// strictly inside a repository root or the worktree root. Implemented over System.IO at the boundary and faked in tests.
/// </summary>
public interface IFolderDeleter
{
    /// <summary>
    /// Deletes a folder tree, including read-only files such as git objects. A missing folder is not an error; a drive
    /// root, a share root or a relative path is refused with an IOException before anything is touched.
    /// </summary>
    void DeleteTree(string path);
}