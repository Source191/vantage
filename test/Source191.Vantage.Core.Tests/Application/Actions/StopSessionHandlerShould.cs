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

    [Fact]
    public async Task StopTheSessionByJobId()
    {
        StopSessionHandler handler = new(new ClaudeCli(processes, Claude));

        Outcome outcome = await handler.HandleAsync(new StopSession(As.Job(Background(Repository, isWorktree: false))), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["stop", "81cdb0f9"]);
        outcome.ShouldBe(new StopSessionOutcome("fix-123"));
    }
}