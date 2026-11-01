using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
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
        processTable.InTree(1, "WindowsTerminal", 0, Noon.AddHours(-1)).InTree(4000, "pwsh", 1, Noon.AddMinutes(-1)).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBe([4000]);
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe(["repos-f6"]);
        close.Ended.ShouldBe([]);
        close.Skipped.ShouldBe([]);
    }

    [Fact]
    public async Task EndClaudeAloneWhenItIsNotInsideAWindowsTerminalTab()
    {
        processTable.InTree(4242, "claude", 9);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBeEmpty();
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([]);
        close.Ended.ShouldBe(["repos-f6"]);
        close.Skipped.ShouldBe([]);
    }

    [Fact]
    public async Task LeaveASessionWhoseProcessIdNowBelongsToAnotherProcess()
    {
        // The listed process id was reused: the process behind it started ten minutes after the session did.
        FakeProcessRegistry reused = new FakeProcessRegistry().Running(4242, Noon.AddMinutes(10)).InTree(1, "WindowsTerminal", 0).InTree(4000, "pwsh", 1).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(reused, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        reused.Terminated.ShouldBeEmpty();
        reused.EndedCleanly.ShouldBeEmpty();
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([]);
        close.Ended.ShouldBe([]);
        close.Gone.ShouldBe(["repos-f6"]);
    }

    [Fact]
    public async Task SkipTerminalSessionsThatHaveMessagesByTheTimeItRuns()
    {
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts().WithMessages("sess-9"));

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-31", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBeEmpty();
        processTable.EndedCleanly.ShouldBeEmpty();
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([]);
        close.Ended.ShouldBe([]);
        close.Skipped.ShouldBe([new SkippedSession("repos-31")]);
    }
}