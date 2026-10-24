using Source191.Vantage.Adapters.Files;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Deletes real folders on a throwaway root, the way a leftover worktree is removed.</summary>
[Trait("Category", "Integration")]
public sealed class FolderDeletionShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-deletion-" + Guid.NewGuid().ToString("N"));

    private string Tree => Path.Combine(root, "Northwind.Orders-fix");

    [Fact]
    public void DeleteAFolderWithReadOnlyFiles()
    {
        string pack = WriteReadOnlyFile(Path.Combine(Tree, ".git", "objects", "pack"), "pack-1.idx");

        FolderDeletion.DeleteTree(Tree);

        File.Exists(pack).ShouldBeFalse();
        Directory.Exists(Tree).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(root, recursive: true);
    }

    private static string WriteReadOnlyFile(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, name);
        File.WriteAllText(file, name);
        File.SetAttributes(file, FileAttributes.ReadOnly);

        return file;
    }
}
