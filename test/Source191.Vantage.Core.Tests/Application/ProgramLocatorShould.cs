using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// A program is found in the PATH folders and started by its full path, so
/// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">Windows
/// never resolves a bare name against the folder Vantage was started from</see>, and never a batch file.
/// </summary>
public sealed class ProgramLocatorShould
{
    private static readonly string[] PathFolders = [@"C:\Windows\System32", @"C:\Program Files\Git\cmd", @"C:\Program Files\GitHub CLI"];

    [Fact]
    public void FindAProgramInTheFirstPathFolderThatHoldsIt()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\Git\cmd\git.exe").WithFile(@"C:\Program Files\GitHub CLI\git.exe");

        new ProgramLocator(folders, PathFolders).Locate("git").ShouldBe(new ProgramLocation.Found(@"C:\Program Files\Git\cmd\git.exe"));
    }

    [Fact]
    public void TakeANameGivenWithItsExtension()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Windows\System32\wt.exe");

        new ProgramLocator(folders, PathFolders).Locate("wt.exe").ShouldBe(new ProgramLocation.Found(@"C:\Windows\System32\wt.exe"));
    }

    [Fact]
    public void NeverAnswerABatchFile()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\GitHub CLI\gh.cmd").WithFile(@"C:\Program Files\GitHub CLI\gh.bat");

        new ProgramLocator(folders, PathFolders).Locate("gh").ShouldBe(new ProgramLocation.Missing());
        new ProgramLocator(folders, PathFolders).Locate("gh.cmd").ShouldBe(new ProgramLocation.Missing());
    }

    [Fact]
    public void SkipARelativePathEntry()
    {
        FakeFolders folders = new FakeFolders().WithFile(@".\git.exe").WithFile(@"bin\git.exe").WithFile(@"C:\Program Files\Git\cmd\git.exe");

        new ProgramLocator(folders, [".", "bin", @"C:\Program Files\Git\cmd"]).Locate("git").ShouldBe(new ProgramLocation.Found(@"C:\Program Files\Git\cmd\git.exe"));
    }

    [Fact]
    public void NeverLookOutsideThePathFolders()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Users\me\source\repos\Planted\git.exe");

        new ProgramLocator(folders, PathFolders).Locate("git").ShouldBe(new ProgramLocation.Missing());
    }

    [Fact]
    public void AnswerMissingForAProgramThatIsNotInstalled()
    {
        new ProgramLocator(new FakeFolders(), PathFolders).Locate("pwsh").ShouldBe(new ProgramLocation.Missing());
    }
}