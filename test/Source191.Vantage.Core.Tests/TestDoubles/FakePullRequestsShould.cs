using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.TestDoubles;

public sealed class FakePullRequestsShould
{
    private static readonly GitHubRepository Orders = new("northwind", "Northwind.Orders");
    private static readonly FolderPath Checkout = new(@"C:\Users\me\source\repos\Northwind.Orders");

    [Fact]
    public async Task HoldACallOpenUntilReleased()
    {
        PullRequestListing.Unavailable signedOut = new(PullRequestGap.NotSignedIn);
        FakePullRequests hosts = new FakePullRequests().Answer(Orders, signedOut).Hold();

        Task<PullRequestListing> call = hosts.ListAsync(Orders, Checkout, CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        call.IsCompleted.ShouldBeFalse();
        hosts.Calls.ShouldBe([Orders]);

        hosts.Release();

        (await call).ShouldBe(signedOut);
    }

    [Fact]
    public async Task AnswerAnEmptyListingForAnUnknownRepository()
    {
        FakePullRequests hosts = new();

        PullRequestListing listing = await hosts.ListAsync(Orders, Checkout, CancellationToken.None);

        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldBeEmpty();
    }
}