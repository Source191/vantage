using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class StopSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeProcessRegistry registry = new();
    private readonly InMemoryState store = new();

    private StopSessionHandler Handler => new(new ClaudeCli(processes, Claude), new SessionTabs(store, registry, new FakeTimeProvider(Noon)));

    [Fact]
    public async Task StopTheSessionByJobId()
    {
        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["stop", "81cdb0f9"]);
        outcome.ShouldBe(new StopSessionOutcome("fix-123", TabClosed: false));
    }

    [Fact]
    public async Task CloseTheTabAfterStopping()
    {
        await LiveTabAsync(store, registry, shell: 4000);
        List<string> order = [];
        registry.OnEndCleanly = shell => order.Add($"shell {shell} ended after {processes.CapturedRequests.Count(request => request.FileName == Claude)} claude call");

        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        order.ShouldBe(["shell 4000 ended after 1 claude call"]);
        outcome.ShouldBe(new StopSessionOutcome("fix-123", TabClosed: true));
    }

    [Fact]
    public async Task SayNothingAboutATabWhenThereIsNone()
    {
        Outcome outcome = await Handler.HandleAsync(new StopSession(Job(Repository)), CancellationToken.None);

        registry.EndedCleanly.ShouldBeEmpty();
        outcome.ShouldBe(new StopSessionOutcome("fix-123", TabClosed: false));
    }
}