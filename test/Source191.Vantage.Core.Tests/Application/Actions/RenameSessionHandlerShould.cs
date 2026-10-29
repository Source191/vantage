using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class RenameSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeTranscripts transcripts = new();
    private readonly InMemoryState store = new();
    private readonly FakeProcessRegistry registry = new();
    private readonly FakeWindowsTerminal terminal = new();

    private RenameSessionHandler Handler
    {
        get
        {
            FakeTimeProvider clock = new(Noon);
            OpenTabHandler openTab = new(store, registry, terminal, clock, new TabScripts(Claude, @"C:\Users\me\.dotnet\tools\vantage.exe"));

            return new(new ClaudeCli(processes, Claude), transcripts, store, new SessionTabs(store, registry, clock), openTab);
        }
    }

    private static RenameSession Rename => new(Job(Repository, DisplayState.Idle), SessionName.From("fix-124"));

    [Fact]
    public async Task RenameBySwappingTheJobForOneResumedUnderTheNewNameKeepingTheSessionId()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-124", string.Empty));
        transcripts.WithMessages("sess-1");

        Outcome message = await Handler.HandleAsync(Rename, CancellationToken.None);

        List<ProcessRequest> claude = [.. processes.CapturedRequests.Where(request => request.FileName == Claude)];
        claude.Select(request => string.Join(' ', request.Arguments)).ShouldBe(["rm 81cdb0f9", "--bg --resume sess-1 -n fix-124"]);
        claude[1].WorkingDirectory.ShouldBe(Repository);
        message.ShouldBe(new RenameSessionOutcome.Renamed("fix-123", "fix-124", TabMoved: false));
    }

    [Fact]
    public async Task RefuseToRenameASessionWithNoTranscriptBecauseTheSwapWouldLoseIt()
    {
        Outcome message = await Handler.HandleAsync(Rename, CancellationToken.None);

        message.ShouldBe(new RenameSessionOutcome.NoMessages("fix-123"));
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutTheOldNameBackWhenTheResumeUnderTheNewNameFails()
    {
        processes.Answer(request => request.Arguments.Contains("fix-124"), new ProcessResult(1, string.Empty, "boom"));
        processes.Answer(request => request.Arguments.Contains("fix-123"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");

        Outcome message = await Handler.HandleAsync(Rename, CancellationToken.None);

        processes.CapturedRequests.Where(request => request.FileName == Claude).Select(request => request.Arguments[^1]).ShouldBe(["81cdb0f9", "fix-124", "fix-123"]);
        message.ShouldBeOfType<RenameSessionOutcome.Restored>().OldName.ShouldBe("fix-123");
    }

    [Fact]
    public async Task MoveTheTab()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-124", string.Empty));
        transcripts.WithMessages("sess-1");
        await LiveTabAsync(store, registry, shell: 4000, colour: TabPalette.Default[3]);
        List<string> order = [];
        registry.OnEndCleanly = shell => order.Add($"shell {shell} ended after {ClaudeCalls()}");

        Outcome message = await Handler.HandleAsync(Rename, CancellationToken.None);

        order.ShouldBe(["shell 4000 ended after rm 81cdb0f9"]);
        ClaudeCalls().ShouldBe("rm 81cdb0f9, --bg --resume sess-1 -n fix-124");
        TabLaunch reopened = terminal.Opened.ShouldHaveSingleItem();
        reopened.Title.ShouldBe("fix-124");
        reopened.Colour.ShouldBe(TabPalette.Default[3]);
        message.ShouldBe(new RenameSessionOutcome.Renamed("fix-123", "fix-124", TabMoved: true));
    }

    [Fact]
    public async Task CloseTheTabWhenTheResumeFails()
    {
        processes.Answer(request => request.Arguments.Contains("fix-124"), new ProcessResult(1, string.Empty, "boom"));
        processes.Answer(request => request.Arguments.Contains("fix-123"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");
        await LiveTabAsync(store, registry, shell: 4000);

        Outcome message = await Handler.HandleAsync(Rename, CancellationToken.None);

        registry.EndedCleanly.ShouldBe([4000]);
        terminal.Opened.ShouldBeEmpty();
        message.ShouldBe(new RenameSessionOutcome.Restored("fix-123", "boom", TabClosed: true));
    }

    private string ClaudeCalls() => string.Join(", ", processes.CapturedRequests.Where(request => request.FileName == Claude).Select(request => string.Join(' ', request.Arguments)));
}