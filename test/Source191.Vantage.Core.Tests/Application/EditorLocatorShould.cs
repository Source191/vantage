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

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(@"D:\Tools\VSCode\Code.exe");
    }

    [Fact]
    public async Task FallBackToTheUserInstallFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Install}\Code.exe");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe($@"{Install}\Code.exe");
    }

    [Fact]
    public async Task FallBackToTheMachineInstallFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Program Files\Microsoft VS Code\Code.exe");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBe(@"C:\Program Files\Microsoft VS Code\Code.exe");
    }

    [Fact]
    public async Task AnswerNullWhenNotInstalled()
    {
        foreach (EditorPreset preset in new[] { EditorPreset.VsCode, EditorPreset.Cursor, EditorPreset.Rider, EditorPreset.VisualStudio, EditorPreset.Custom })
        {
            (await Locate(new FakeFolders(), preset)).ShouldBeNull(preset.ToString());
        }
    }

    [Fact]
    public async Task NeverAnswerACmdFile()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"D:\Tools\VSCode\bin\code.cmd");

        (await Locate(folders, EditorPreset.VsCode)).ShouldBeNull();
    }

    [Fact]
    public async Task FindCursorInTheUserInstallFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile(@"C:\Users\me\AppData\Local\Programs\cursor\Cursor.exe");

        (await Locate(folders, EditorPreset.Cursor)).ShouldBe(@"C:\Users\me\AppData\Local\Programs\cursor\Cursor.exe");
    }

    [Fact]
    public async Task FindTheNewestRiderFromEitherInstallPlace()
    {
        FakeFolders folders = new FakeFolders()
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2024.3\bin\rider64.exe")
            .WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe")
            .WithFile(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\251.1\bin\rider64.exe");

        (await Locate(folders, EditorPreset.Rider)).ShouldBe(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\251.1\bin\rider64.exe");
        (await Locate(new FakeFolders().WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe").WithFile(@"C:\Program Files\JetBrains\JetBrains Rider 2024.3\bin\rider64.exe"), EditorPreset.Rider))
            .ShouldBe(@"C:\Program Files\JetBrains\JetBrains Rider 2025.2\bin\rider64.exe");
    }

    [Fact]
    public async Task AskVswhereForVisualStudio()
    {
        const string vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        const string devenv = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
        FakeFolders folders = new FakeFolders().WithFile(vswhere).WithFile(devenv);
        processes.AnswerAll(new ProcessResult(0, devenv + "\r\n", string.Empty));

        (await Locate(folders, EditorPreset.VisualStudio)).ShouldBe(devenv);
        processes.CapturedRequests.Single().Arguments.ShouldBe(["-latest", "-property", "productPath"]);
    }

    [Fact]
    public async Task AnswerExplorerFromTheWindowsFolder()
    {
        (await Locate(new FakeFolders(), EditorPreset.Explorer)).ShouldBe(@"C:\Windows\explorer.exe");
    }

    private Task<string?> Locate(FakeFolders folders, EditorPreset preset)
    {
        return new EditorLocator(folders, processes, Where).LocateAsync(preset, CancellationToken.None);
    }
}