using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.TestDoubles;

public sealed class FakeFoldersShould
{
    [Theory]
    [InlineData(@"C:\")]
    [InlineData("C:")]
    [InlineData(@"\\server\share\")]
    [InlineData(@"repos\Northwind.Orders")]
    public void RefuseToDeleteARootOrARelativePath(string path)
    {
        FakeFolders folders = new FakeFolders().WithDirectory(@"C:\repos\Northwind.Orders");

        Should.Throw<IOException>(() => folders.DeleteTree(path));

        folders.DirectoryExists(@"C:\repos\Northwind.Orders").ShouldBeTrue();
    }

    [Fact]
    public void ThrowForAnUndeclaredFolder()
    {
        FakeFolders folders = new();

        Should.Throw<DirectoryNotFoundException>(() => folders.Directories(@"C:\repos").ToList());
        Should.Throw<DirectoryNotFoundException>(() => folders.Files(@"C:\repos").ToList());
        Should.Throw<DirectoryNotFoundException>(() => folders.IsEmpty(@"C:\repos"));
        folders.FilesIn(@"C:\repos").ShouldBeEmpty();
    }

    [Fact]
    public void ThrowForAMissingFile()
    {
        FakeFolders folders = new FakeFolders().WithDirectory(@"C:\repos");

        Should.Throw<FileNotFoundException>(() => folders.Lines(@"C:\repos\missing.txt").ToList());
    }
}