using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Deletes real folders on a throwaway root, the way a leftover worktree is removed.</summary>
[Trait("Category", "Integration")]
public sealed class FolderDeletionShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-deletion-" + Guid.NewGuid().ToString("N"));

    private readonly List<string> junctions = [];

    private string Tree => Path.Combine(root, "Northwind.Orders-fix");

    [Fact]
    public void DeleteAFolderWithReadOnlyFiles()
    {
        string pack = WriteReadOnlyFile(Path.Combine(Tree, ".git", "objects", "pack"), "pack-1.idx");

        FolderDeletion.DeleteTree(Tree);

        File.Exists(pack).ShouldBeFalse();
        Directory.Exists(Tree).ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveTheTargetOfAJunctionUntouched()
    {
        string kept = WriteReadOnlyFile(Path.Combine(root, "shared-store", "pkg"), "index.js");
        WriteReadOnlyFile(Path.Combine(Tree, "src"), "Program.cs");
        await CreateJunctionAsync(Path.Combine(Tree, "node_modules", "pkg"), Path.Combine(root, "shared-store", "pkg"));

        FolderDeletion.DeleteTree(Tree);

        Directory.Exists(Tree).ShouldBeFalse();
        File.Exists(kept).ShouldBeTrue();
        File.GetAttributes(kept).HasFlag(FileAttributes.ReadOnly).ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAFolderThatIsItselfAJunction()
    {
        string kept = WriteReadOnlyFile(Path.Combine(root, "Northwind.Orders", "src"), "Program.cs");
        await CreateJunctionAsync(Tree, Path.Combine(root, "Northwind.Orders"));

        FolderDeletion.DeleteTree(Tree);

        Directory.Exists(Tree).ShouldBeFalse();
        File.Exists(kept).ShouldBeTrue();
        File.GetAttributes(kept).HasFlag(FileAttributes.ReadOnly).ShouldBeTrue();
    }

    /// <summary>
    /// The refusal tests aim only at roots and folders that do not exist, so without the guard the deletion returns
    /// quietly and the test fails on the missing exception; nothing real is ever at risk.
    /// </summary>
    [Fact]
    public void RefuseADriveRoot()
    {
        string drive = UnusedDriveRoot();

        IOException refusal = Should.Throw<IOException>(() => FolderDeletion.DeleteTree(drive));

        refusal.Message.ShouldContain(drive);
    }

    [Fact]
    public void RefuseAShareRoot()
    {
        const string share = @"\\vantage-no-such-host\share\";

        IOException refusal = Should.Throw<IOException>(() => FolderDeletion.DeleteTree(share));

        refusal.Message.ShouldContain(share);
    }

    [Fact]
    public void RefuseARelativePath()
    {
        string relative = "Northwind.Orders-" + Guid.NewGuid().ToString("N");

        IOException refusal = Should.Throw<IOException>(() => FolderDeletion.DeleteTree(relative));

        refusal.Message.ShouldContain(relative);
    }

    [Fact]
    public void DeleteAFolderWrittenWithATrailingSeparator()
    {
        string pack = WriteReadOnlyFile(Path.Combine(Tree, ".git", "objects", "pack"), "pack-1.idx");

        FolderDeletion.DeleteTree(Tree + Path.DirectorySeparatorChar);

        File.Exists(pack).ShouldBeFalse();
        Directory.Exists(Tree).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (string junction in junctions.Where(Directory.Exists))
        {
            Directory.Delete(junction);
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(root, recursive: true);
    }

    /// <summary>Junctions need neither elevation nor Developer Mode, unlike symbolic links, and .NET cannot make one.</summary>
    private async Task CreateJunctionAsync(string link, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        ProcessRequest request = new("cmd.exe", ["/c", "mklink", "/J", link, target], WorkingDirectory: null);
        ProcessResult result = await new ProcessRunner().RunAsync(request, TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
        junctions.Add(link);
    }

    /// <summary>The first drive letter from Z down to D that this machine does not have, as a root such as Q:\.</summary>
    private static string UnusedDriveRoot()
    {
        HashSet<char> used = [.. DriveInfo.GetDrives().Select(drive => char.ToUpperInvariant(drive.Name[0]))];
        char letter = Enumerable.Range('D', 'Z' - 'D' + 1).Select(code => (char)code).Reverse().First(candidate => !used.Contains(candidate));

        return $@"{letter}:\";
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