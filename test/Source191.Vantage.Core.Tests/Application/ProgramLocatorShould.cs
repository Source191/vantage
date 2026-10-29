using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// A program is found in the PATH folders and started by its full path, so Windows never resolves a bare name against
/// the folder Vantage was started from, and never a batch file.
/// </summary>
public sealed class ProgramLocatorShould
{
    private static readonly string[] PathFolders = [@"C:\Windows\System32", @"C:\Program Files\Git\cmd", @"C:\Program Files\GitHub CLI"];

    [Fact]
    public void FindAProgramInTheFirstPathFolderThatHoldsIt()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\Git\cmd\git.exe").WithFile(@"C:\Program Files\GitHub CLI\git.exe");

        new ProgramLocator(folders, PathFolders).Locate("git").ShouldBe(@"C:\Program Files\Git\cmd\git.exe");
    }

    [Fact]
    public void TakeANameGivenWithItsExtension()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Windows\System32\wt.exe");

        new ProgramLocator(folders, PathFolders).Locate("wt.exe").ShouldBe(@"C:\Windows\System32\wt.exe");
    }

    [Fact]
    public void NeverAnswerABatchFile()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\GitHub CLI\gh.cmd").WithFile(@"C:\Program Files\GitHub CLI\gh.bat");

        new ProgramLocator(folders, PathFolders).Locate("gh").ShouldBeNull();
        new ProgramLocator(folders, PathFolders).Locate("gh.cmd").ShouldBeNull();
    }

    [Fact]
    public void NeverLookOutsideThePathFolders()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Users\me\source\repos\Planted\git.exe");

        new ProgramLocator(folders, PathFolders).Locate("git").ShouldBeNull();
    }

    [Fact]
    public void AnswerNullForAProgramThatIsNotInstalled()
    {
        new ProgramLocator(new FakeFolders(), PathFolders).Locate("pwsh").ShouldBeNull();
    }
}
