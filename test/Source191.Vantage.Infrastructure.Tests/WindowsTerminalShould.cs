using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class WindowsTerminalShould
{
    private const string Wt = @"C:\Users\me\AppData\Local\Microsoft\WindowsApps\wt.exe";

    private static readonly TabLaunch Launch = new("Title", TabPalette.Default[0], FolderPath.From(@"C:\repos\Northwind.Orders"), "Write-Host hi");

    [Fact]
    public async Task OpenATabByRunningWtWithTheBuiltArguments()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), Wt);

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        ProcessRequest request = processes.CapturedRequests.Single();
        request.FileName.ShouldBe(Wt);
        request.Arguments.ShouldBe(WindowsTerminalArguments.ForTab(Launch, profile: null));
    }

    [Fact]
    public async Task OpenTabsInTheSessionProfileWhenTheFragmentExists()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment().Installed(), Wt);

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(WindowsTerminalArguments.ForTab(Launch, "Vantage session"));
    }

    [Fact]
    public async Task OpenTabsInTheDefaultProfileOtherwise()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), Wt);

        await terminal.OpenTabAsync(Launch, CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldNotContain("--profile");
    }

    [Fact]
    public async Task ThrowWhenWindowsTerminalRefusesTheTab()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(1, string.Empty, "bad args"));
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), Wt);

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(Launch, CancellationToken.None));

        exception.Message.ShouldContain("bad args");
    }

    [Fact]
    public async Task ThrowNamingWtWhenItIsNotOnThePath()
    {
        FakeProcessRunner processes = new();
        WindowsTerminal terminal = new(processes, new FakeTerminalFragment(), executable: null);

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(Launch, CancellationToken.None));

        exception.Message.ShouldContain("wt.exe");
        processes.CapturedRequests.ShouldBeEmpty();
    }
}
