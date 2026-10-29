using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>Claude Code is run from its native install or a claude.exe on the PATH, and never through a batch file.</summary>
public sealed class ClaudeExecutableShould
{
    private const string Home = @"C:\Users\me";
    private const string Native = @"C:\Users\me\.local\bin\claude.exe";
    private const string Shim = @"C:\Users\me\AppData\Roaming\npm\claude.cmd";

    private static readonly string[] PathFolders = [@"C:\Users\me\AppData\Roaming\npm", @"C:\Tools"];

    [Fact]
    public void PreferTheNativeInstallToThePath()
    {
        FakeFolders folders = new FakeFolders().WithFile(Native).WithFile(@"C:\Tools\claude.exe").WithFile(Shim);

        ClaudeExecutable.Locate(folders, PathFolders, Home).ShouldBe(new ClaudeLocation.Found(Native));
    }

    [Fact]
    public void FindClaudeExeOnThePath()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Tools\claude.exe");

        ClaudeExecutable.Locate(folders, PathFolders, Home).ShouldBe(new ClaudeLocation.Found(@"C:\Tools\claude.exe"));
    }

    [Fact]
    public void PreferAnExeLaterOnThePathToAShimEarlier()
    {
        FakeFolders folders = new FakeFolders().WithFile(Shim).WithFile(@"C:\Tools\claude.exe");

        ClaudeExecutable.Locate(folders, PathFolders, Home).ShouldBe(new ClaudeLocation.Found(@"C:\Tools\claude.exe"));
    }

    [Fact]
    public void RefuseAShimAloneNamingIt()
    {
        FakeFolders folders = new FakeFolders().WithFile(Shim);

        ClaudeExecutable.Locate(folders, PathFolders, Home).ShouldBe(new ClaudeLocation.BatchFileOnly(Shim));
    }

    [Fact]
    public void AnswerMissingWhenNothingIsInstalled()
    {
        ClaudeExecutable.Locate(new FakeFolders(), PathFolders, Home).ShouldBe(new ClaudeLocation.Missing());
    }
}
