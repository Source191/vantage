using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The real file system behind IFolders, on a throwaway folder.</summary>
[Trait("Category", "Integration")]
public sealed class FoldersShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-folders-" + Guid.NewGuid().ToString("N"));
    private readonly Folders folders = new();
    private readonly List<string> junctions = [];

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

    [Fact]
    public async Task ListNoFileBehindAJunction()
    {
        string shared = Path.Combine(root, "shared-store");
        Directory.CreateDirectory(shared);
        await File.WriteAllTextAsync(Path.Combine(shared, "index.js"), "// shared", TestContext.Current.CancellationToken);
        string link = Path.Combine(root, "Northwind.Orders", "node_modules", "pkg");
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        ProcessResult result = await new ProcessRunner().RunAsync(new ProcessRequest("cmd.exe", ["/c", "mklink", "/J", link, shared], WorkingFolder.Inherit), TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
        junctions.Add(link);

        folders.Files(Path.Combine(root, "Northwind.Orders")).Select(Path.GetFileName).ShouldBe([".git", "Program.cs"], ignoreOrder: true);
    }

    public void Dispose()
    {
        foreach (string junction in junctions.Where(Directory.Exists))
        {
            Directory.Delete(junction);
        }

        Directory.Delete(root, recursive: true);
    }
}