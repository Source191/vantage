using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class OpenPullRequestHandlerShould
{
    [Fact]
    public async Task OpenTheUrlAndReportIt()
    {
        FakeDesktop desktop = new();
        OpenPullRequestHandler handler = new(desktop);
        PullRequest pullRequest = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521"), PullRequestState.Active, DateTimeOffset.UnixEpoch);

        Outcome outcome = await handler.HandleAsync(new OpenPullRequest(pullRequest), CancellationToken.None);

        desktop.Opened.ShouldBe(["https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521"]);
        outcome.ShouldBe(new OpenPullRequestOutcome(PullRequestHost.AzureDevOps, 4521));
    }
}