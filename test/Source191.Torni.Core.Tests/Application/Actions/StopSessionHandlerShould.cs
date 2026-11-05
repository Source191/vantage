using Source191.Torni.Adapters.Claude;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;
using static Source191.Torni.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class StopSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeProcessRegistry registry = new();
    private readonly InMemoryState store = new();

    private StopSessionHandler Handler => new(new ClaudeCli(processes, Claude), new SessionTabs(store, registry));

    [Fact]
    public async Task StopTheSessionByJobId()
    {
        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["stop", "81cdb0f9"]);
        outcome.ShouldBe(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: false));
    }

    [Fact]
    public async Task CloseTheTabAfterStopping()
    {
        await LiveTabAsync(store, registry, shell: 4000);
        List<string> order = [];
        registry.OnEndCleanly = shell => order.Add($"shell {shell} ended after {processes.CapturedRequests.Count(request => request.FileName == Claude)} claude call");

        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        order.ShouldBe(["shell 4000 ended after 1 claude call"]);
        outcome.ShouldBe(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: true));
    }

    [Fact]
    public async Task SayNothingAboutATabWhenThereIsNone()
    {
        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        registry.EndedCleanly.ShouldBeEmpty();
        outcome.ShouldBe(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: false));
    }

    [Fact]
    public async Task ReportAStopClaudeRefusedAndKeepTheTab()
    {
        await LiveTabAsync(store, registry, shell: 4000);
        processes.Answer(request => request.Arguments.Contains("stop"), new ProcessResult(1, string.Empty, "Session 81cdb0f9 not found"));

        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        outcome.ShouldBe(new StopSessionOutcome.NotStopped(Listed.As("fix-123"), "Session 81cdb0f9 not found"));
        registry.EndedCleanly.ShouldBeEmpty();
    }
}