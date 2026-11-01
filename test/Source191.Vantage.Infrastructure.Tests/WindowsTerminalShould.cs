using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class WindowsTerminalShould
{
    private const string Wt = @"C:\Users\me\AppData\Local\Microsoft\WindowsApps\wt.exe";
    private const string Pwsh = @"C:\Program Files\PowerShell\7\pwsh.exe";

    private static readonly TabLaunch Launch = new("Title", TabPalette.Default[0], FolderPath.From(@"C:\repos\Northwind.Orders"), "Write-Host hi");

    [Fact]
    public async Task OpenATabByRunningWtWithTheBuiltArguments()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), new ProgramLocation.Found(Wt), new ProgramLocation.Found(Pwsh));

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        ProcessRequest request = processes.CapturedRequests.Single();
        request.FileName.ShouldBe(Wt);
        request.Arguments.ShouldBe(WindowsTerminalArguments.ForTab(Launch, profile: [], Pwsh));
    }

    [Fact]
    public async Task OpenTabsInTheSessionProfileWhenTheFragmentExists()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment().Installed(), new ProgramLocation.Found(Wt), new ProgramLocation.Found(Pwsh));

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(WindowsTerminalArguments.ForTab(Launch, ["--profile", "Vantage session"], Pwsh));
    }

    [Fact]
    public async Task OpenTabsInTheDefaultProfileOtherwise()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), new ProgramLocation.Found(Wt), new ProgramLocation.Found(Pwsh));

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldNotContain("--profile");
    }

    [Fact]
    public async Task ThrowWhenWindowsTerminalRefusesTheTab()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(1, string.Empty, "bad args"));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), new ProgramLocation.Found(Wt), new ProgramLocation.Found(Pwsh));

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(Launch, CancellationToken.None));

        exception.Message.ShouldContain("bad args");
    }

    [Fact]
    public async Task RefuseToOpenATabWithoutWt()
    {
        FakeProcessRunner processes = new();
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), new ProgramLocation.Missing(), new ProgramLocation.Found(Pwsh));

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(Launch, CancellationToken.None));

        exception.Message.ShouldBe("Windows Terminal (wt.exe) was not found on the PATH, so no tab can be opened.");
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ThrowNamingPwshWhenItIsNotOnThePath()
    {
        FakeProcessRunner processes = new();
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), new ProgramLocation.Found(Wt), new ProgramLocation.Missing());

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(Launch, CancellationToken.None));

        exception.Message.ShouldBe("PowerShell 7 (pwsh.exe) was not found on the PATH, so no tab can be opened.");
        processes.CapturedRequests.ShouldBeEmpty();
    }
}