using Source191.Vantage.Adapters.Files;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The real file system behind IFolders, on a throwaway folder.</summary>
[Trait("Category", "Integration")]
public sealed class FoldersShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-folders-" + Guid.NewGuid().ToString("N"));
    private readonly Folders folders = new();

    public FoldersShould()
    {
        Directory.CreateDirectory(Path.Combine(root, "Northwind.Orders", "src"));
        Directory.CreateDirectory(Path.Combine(root, "empty"));
        File.WriteAllText(Path.Combine(root, "Northwind.Orders", ".git"), "gitdir: ../Northwind.Orders-main/.git/worktrees/x\n");
        File.WriteAllText(Path.Combine(root, "Northwind.Orders", "src", "Program.cs"), "// code");
    }

    [Fact]
    public void AnswerExistenceForFoldersAndFiles()
    {
        folders.DirectoryExists(Path.Combine(root, "Northwind.Orders")).ShouldBeTrue();
        folders.DirectoryExists(Path.Combine(root, "missing")).ShouldBeFalse();
        folders.FileExists(Path.Combine(root, "Northwind.Orders", ".git")).ShouldBeTrue();
        folders.FileExists(Path.Combine(root, "Northwind.Orders")).ShouldBeFalse();
    }

    [Fact]
    public void ListDirectChildFoldersAndEveryFileBeneath()
    {
        folders.Directories(root).Select(Path.GetFileName).ShouldBe(["empty", "Northwind.Orders"], ignoreOrder: true);
        folders.Files(Path.Combine(root, "Northwind.Orders")).Select(Path.GetFileName).ShouldBe([".git", "Program.cs"], ignoreOrder: true);
    }

    [Fact]
    public void ListOnlyTheFilesDirectlyInAFolder()
    {
        folders.FilesIn(Path.Combine(root, "Northwind.Orders")).Select(Path.GetFileName).ShouldBe([".git"]);
        folders.FilesIn(Path.Combine(root, "Northwind.Orders", "src")).Select(Path.GetFileName).ShouldBe(["Program.cs"]);
        folders.FilesIn(Path.Combine(root, "missing")).ShouldBeEmpty();
    }

    [Fact]
    public void KnowAnEmptyFolderAndReadLines()
    {
        folders.IsEmpty(Path.Combine(root, "empty")).ShouldBeTrue();
        folders.IsEmpty(Path.Combine(root, "Northwind.Orders")).ShouldBeFalse();
        folders.Lines(Path.Combine(root, "Northwind.Orders", ".git")).First().ShouldBe("gitdir: ../Northwind.Orders-main/.git/worktrees/x");
    }

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
    }
}