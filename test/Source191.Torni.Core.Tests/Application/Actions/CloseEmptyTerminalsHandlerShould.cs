using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class CloseEmptyTerminalsHandlerShould
{
    private readonly FakeProcessRegistry processTable = new FakeProcessRegistry().Running(4242, Fixtures.Noon);

    [Fact]
    public async Task LeaveAnotherTabsShellWhenTheParentIdIsReused()
    {
        // The launching shell exited; its id now belongs to a pwsh that started later, the root of another tab.
        processTable.InTree(1, "WindowsTerminal", 0, Fixtures.Noon.AddHours(-1)).InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(5)).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBeEmpty();
    }

    [Fact]
    public async Task CloseTheTabOfATerminalSessionWithNoMessagesByEndingClaudeThenItsShellCleanly()
    {
        // WindowsTerminal (1) owns the tab's pwsh (4000), which started claude (4242); ending the shell with exit code 0 closes the tab.
        processTable.InTree(1, "WindowsTerminal", 0, Fixtures.Noon.AddHours(-1)).InTree(4000, "pwsh", 1, Fixtures.Noon.AddMinutes(-1)).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBe([4000]);
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([Listed.As("repos-f6")]);
        close.Ended.ShouldBe([]);
        close.Skipped.ShouldBe([]);
    }

    [Fact]
    public async Task CloseTheTabOfAClaudeThatWindowsTerminalStartedDirectlyByEndingItCleanly()
    {
        processTable.InTree(1, "WindowsTerminal", 0, Fixtures.Noon.AddHours(-1)).InTree(4242, "claude", 1);
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        processTable.EndedCleanly.ShouldBe([4242]);
        processTable.Terminated.ShouldBeEmpty();
        message.ShouldBeOfType<CloseEmptyTerminalsOutcome>().Closed.ShouldBe([Listed.As("repos-f6")]);
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
        close.Ended.ShouldBe([Listed.As("repos-f6")]);
        close.Skipped.ShouldBe([]);
    }

    [Fact]
    public async Task LeaveASessionWhoseProcessIdNowBelongsToAnotherProcess()
    {
        // The listed process id was reused: the process behind it started ten minutes after the session did.
        FakeProcessRegistry reused = new FakeProcessRegistry().Running(4242, Fixtures.Noon.AddMinutes(10)).InTree(1, "WindowsTerminal", 0).InTree(4000, "pwsh", 1).InTree(4242, "claude", 4000);
        CloseEmptyTerminalsHandler handler = new(reused, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-f6", 4242)]), CancellationToken.None);

        reused.Terminated.ShouldBeEmpty();
        reused.EndedCleanly.ShouldBeEmpty();
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([]);
        close.Ended.ShouldBe([]);
        close.Gone.ShouldBe([Listed.As("repos-f6")]);
    }

    [Fact]
    public async Task SkipTerminalSessionsThatHaveMessagesByTheTimeItRuns()
    {
        CloseEmptyTerminalsHandler handler = new(processTable, new FakeTranscripts().WithMessages(SessionId.From("sess-9")));

        Outcome message = await handler.HandleAsync(new CloseEmptyTerminals([SessionTestData.Terminal("repos-31", 4242)]), CancellationToken.None);

        processTable.Terminated.ShouldBeEmpty();
        processTable.EndedCleanly.ShouldBeEmpty();
        CloseEmptyTerminalsOutcome close = message.ShouldBeOfType<CloseEmptyTerminalsOutcome>();
        close.Closed.ShouldBe([]);
        close.Ended.ShouldBe([]);
        close.Skipped.ShouldBe([new SkippedSession(Listed.As("repos-31"))]);
    }
}