using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class WindowsTerminalShould
{
    [Fact]
    public async Task OpenATabByRunningWtWithTheBuiltArguments()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        WindowsTerminal terminal = new(processes);
        TabLaunch launch = new("Title", TabPalette.Default[0], new FolderPath(@"C:\repos\Northwind.Orders"), "Write-Host hi");

        await terminal.OpenTabAsync(launch, CancellationToken.None);

        ProcessRequest request = processes.CapturedRequests.Single();
        request.FileName.ShouldBe("wt.exe");
        request.Arguments.ShouldBe(WindowsTerminalArguments.ForTab(launch));
    }

    [Fact]
    public async Task ThrowWhenWindowsTerminalRefusesTheTab()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(1, string.Empty, "bad args"));
        WindowsTerminal terminal = new(processes);
        TabLaunch launch = new("Title", TabPalette.Default[0], new FolderPath(@"C:\repos\Northwind.Orders"), "Write-Host hi");

        TerminalException exception = await Should.ThrowAsync<TerminalException>(() => terminal.OpenTabAsync(launch, CancellationToken.None));

        exception.Message.ShouldContain("bad args");
    }
}