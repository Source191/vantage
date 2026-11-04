using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.TestDoubles;

public sealed class FakePullRequestsShould
{
    private static readonly RemoteRepository.GitHub Orders = new("northwind", "Northwind.Orders");
    private static readonly FolderPath Checkout = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task HoldACallOpenUntilReleased()
    {
        PullRequestListing.Unavailable signedOut = new(PullRequestGap.NotSignedIn);
        FakePullRequests hosts = new FakePullRequests().Answer(Orders, signedOut).Hold();

        Task<PullRequestListing> call = hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None);
        await hosts.WhenCalledAsync(1).WaitAsync(Deadline, TestContext.Current.CancellationToken);
        call.IsCompleted.ShouldBeFalse();
        hosts.Calls.ShouldBe([Orders]);

        hosts.Release();

        (await call).ShouldBe(signedOut);
    }

    [Fact]
    public async Task AnswerAnEmptyListingForAnUnknownRepository()
    {
        FakePullRequests hosts = new();

        PullRequestListing listing = await hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None);

        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CompleteWhenCalledAsyncWhenTheCountIsReached()
    {
        FakePullRequests hosts = new();
        Task two = hosts.WhenCalledAsync(2);
        Task three = hosts.WhenCalledAsync(3);

        await hosts.ListAsync(Orders, Checkout, BranchName.From("fix-1"), CancellationToken.None);
        two.IsCompleted.ShouldBeFalse();
        await hosts.ListAsync(Orders, Checkout, BranchName.From("fix-2"), CancellationToken.None);

        await two.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        three.IsCompleted.ShouldBeFalse();
        hosts.WhenCalledAsync(1).IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task HoldEachCallAndReleaseThemOneAtATime()
    {
        FakePullRequests hosts = new FakePullRequests().Hold();
        Task<PullRequestListing> first = hosts.ListAsync(Orders, Checkout, BranchName.From("fix-1"), CancellationToken.None);
        Task<PullRequestListing> second = hosts.ListAsync(Orders, Checkout, BranchName.From("fix-2"), CancellationToken.None);
        await hosts.WhenCalledAsync(2).WaitAsync(Deadline, TestContext.Current.CancellationToken);

        hosts.ReleaseOne();

        await first.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        second.IsCompleted.ShouldBeFalse();
        hosts.Release();
        await second.WaitAsync(Deadline, TestContext.Current.CancellationToken);
    }
}