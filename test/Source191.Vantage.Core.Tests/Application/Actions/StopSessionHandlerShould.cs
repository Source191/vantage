using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
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

        string message = await handler.HandleAsync(new StopSession(Background(Repository, isWorktree: false)), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["stop", "81cdb0f9"]);
        message.ShouldBe("Stopped fix-123.");
    }
}