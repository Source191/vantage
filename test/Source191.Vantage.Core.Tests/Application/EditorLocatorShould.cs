using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class EditorLocatorShould
{
    private const string Install = @"C:\Users\me\AppData\Local\Programs\Microsoft VS Code";

    private static readonly EditorFolders Where = new(
        [@"C:\Windows", @"D:\Tools\VSCode\bin"],
        @"C:\Users\me\AppData\Local",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\Windows");

    private readonly FakeProcessRunner processes = new();

    [Fact]
    public async Task FindCodeExeBesideTheCodeCmdShimOnThePath()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"D:\Tools\VSCode\bin\code.cmd").WithFile(@"D:\Tools\VSCode\Code.exe");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(new ProgramLocation.Found(@"D:\Tools\VSCode\Code.exe"));
    }

    [Fact]
    public async Task IgnoreACodeShimInARelativeFolder()
    {
        EditorFolders relative = Where with { PathFolders = [@"tools\VSCode\bin"] };
        FakeFolders folders = new FakeFolders().WithFile(@"tools\VSCode\bin\code.cmd").WithFile(Path.GetFullPath(@"tools\VSCode\Code.exe"));

        (await new EditorLocator(folders, processes, relative).LocateAsync(EditorPreset.VsCode, CancellationToken.None)).ShouldBe(new ProgramLocation.Missing());
    }

    [Fact]
    public async Task FallBackToTheUserInstallFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Install}\Code.exe");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(new ProgramLocation.Found($@"{Install}\Code.exe"));
    }

    [Fact]
    public async Task FallBackToTheMachineInstallFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\Microsoft VS Code\Code.exe");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(new ProgramLocation.Found(@"C:\Program Files\Microsoft VS Code\Code.exe"));
    }

    [Fact]
    public async Task AnswerMissingWhenNotInstalled()
    {
        EditorPreset[] presets = [EditorPreset.VsCode, EditorPreset.VisualStudio, EditorPreset.Rider, EditorPreset.Custom];
        foreach (EditorPreset preset in presets)
        {
            (await Locate(new FakeFolders(), preset)).ShouldBe(new ProgramLocation.Missing(), preset.ToString());
        }
    }

    [Fact]
    public async Task NeverAnswerACmdFile()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"D:\Tools\VSCode\bin\code.cmd");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(new ProgramLocation.Missing());
    }

    [Fact]
    public async Task AskVswhereForTheNewestVisualStudio()
    {
        const string vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        const string devenv = @"C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\devenv.exe";
        FakeFolders folders = new FakeFolders().WithFile(vswhere).WithFile(devenv);
        processes.AnswerAll(new ProcessResult(0, devenv + "\r\n", string.Empty));

        (await Locate(folders, EditorPreset.VisualStudio)).ShouldBe(new ProgramLocation.Found(devenv));
        processes.CapturedRequests.Single().FileName.ShouldBe(vswhere);
        processes.CapturedRequests.Single().Arguments.ShouldBe(["-latest", "-property", "productPath"]);
    }

    [Fact]
    public async Task AnswerMissingWhenVswhereNamesNothingThatExists()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe");
        processes.AnswerAll(new ProcessResult(0, @"C:\Gone\devenv.exe", string.Empty));

        (await Locate(folders, EditorPreset.VisualStudio)).ShouldBe(new ProgramLocation.Missing());
    }

    [Fact]
    public async Task FindTheNewestRiderFromEitherInstallPlace()
    {
        FakeFolders folders = new FakeFolders()
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2024.3\bin\rider64.exe")
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\251.1\bin\rider64.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(new ProgramLocation.Found(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\251.1\bin\rider64.exe"));
        (await Locate(new FakeFolders().WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe").WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2024.3\bin\rider64.exe"), EditorPreset.Rider))
            .ShouldBe(new ProgramLocation.Found(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe"));
    }

    [Fact]
    public async Task RankRiderBuildsByNumberNotByText()
    {
        FakeFolders folders = new FakeFolders()
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.9\bin\rider64.exe")
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.10\bin\rider64.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(new ProgramLocation.Found(@"C:\Program Files\JetBrains\JetBrains Rider 2025.10\bin\rider64.exe"));
    }

    [Theory]
    [InlineData("Rider")]
    [InlineData("JetBrains Rider 2025.3")]
    public async Task FindRiderInTheUserProgramsFolder(string folder)
    {
        string rider = $@"C:\Users\me\AppData\Local\Programs\{folder}\bin\rider64.exe";
        FakeFolders folders = new FakeFolders().WithFile(rider).WithFile(@"C:\Users\me\AppData\Local\Programs\Microsoft VS Code\Code.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(new ProgramLocation.Found(rider));
    }

    [Fact]
    public async Task ChooseTheNewestRiderAcrossEveryInstallFolder()
    {
        FakeFolders folders = new FakeFolders()
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\251.1\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\Programs\Rider 2025.9\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\Programs\Rider 2025.10\bin\rider64.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(new ProgramLocation.Found(@"C:\Users\me\AppData\Local\Programs\Rider 2025.10\bin\rider64.exe"));
    }

    [Fact]
    public async Task RankToolboxBuildsByNumber()
    {
        FakeFolders folders = new FakeFolders()
            .WithFile(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\253.99\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\253.100\bin\rider64.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(new ProgramLocation.Found(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\253.100\bin\rider64.exe"));
    }


    [Fact]
    public async Task AnswerExplorerFromTheWindowsFolder()
    {
        (await Locate(new FakeFolders(), EditorPreset.Explorer)).ShouldBe(new ProgramLocation.Found(@"C:\Windows\explorer.exe"));
    }

    private Task<ProgramLocation> Locate(FakeFolders folders, EditorPreset preset)
    {
        return new EditorLocator(folders, processes, Where).LocateAsync(preset, CancellationToken.None);
    }
}