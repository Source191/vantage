using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class CloseEmptyTerminalsHandlerShould
{
    private readonly FakeProcessRegistry processTable = new FakeProcessRegistry().Running(4242, Noon);

    [Fact]
    public async Task CloseTheTabOfATerminalSessionWithNoMessagesByEndingClaudeThenItsShellCleanly()
    {
        // WindowsTerminal (1) owns the tab's pwsh (4000), which started claude (4242); ending the shell with exit code 0 closes the tab.
        processTable.InTree(1, "WindowsTerminal", 0).InTree(4000, "pwsh", 1).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        string message = await handler.HandleAsync(new CloseEmptyTerminals([Interactive("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBe([4000]);
        message.ShouldBe("Closed 1 tab: repos-f6.");
    }

    [Fact]
    public async Task EndClaudeAloneWhenItIsNotInsideAWindowsTerminalTab()
    {
        processTable.InTree(4242, "claude", 9);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        string message = await handler.HandleAsync(new CloseEmptyTerminals([Interactive("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBeEmpty();
        message.ShouldBe("Ended 1 session: repos-f6 (not in a Windows Terminal tab, so nothing to close).");
    }

    [Fact]
    public async Task SkipTerminalSessionsThatHaveMessagesOrNoProcess()
    {
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts().WithMessages("sess-9"));

        string message = await handler.HandleAsync(new CloseEmptyTerminals([Interactive("repos-31", 4242), Interactive("repos-93", processId: null)]), CancellationToken.None);

        processTable.Terminated.ShouldBeEmpty();
        processTable.EndedCleanly.ShouldBeEmpty();
        message.ShouldBe("Nothing closed: repos-31 has messages (press b to move it under Vantage); repos-93 has no process.");
    }
}