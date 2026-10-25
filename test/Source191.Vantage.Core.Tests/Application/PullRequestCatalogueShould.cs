using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class PullRequestCatalogueShould
{
    private static readonly FolderPath Checkout = new(@"C:\Users\me\source\repos\Northwind.Orders");
    private static readonly GitHubRepository Orders = new("northwind", "Northwind.Orders");
    private static readonly BranchName Fix = new("fix-123");
    private static readonly PullRequest Open = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero));
    private static readonly PullRequest Merged = Open with { State = PullRequestState.Merged };

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .Answer(request => request.Arguments.SequenceEqual(["remote", "get-url", "origin"]), new ProcessResult(0, "git@github.com:northwind/Northwind.Orders.git\n", string.Empty));

    private readonly FakePullRequests hosts = new FakePullRequests().Answer(Orders, Listing(Open));
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));
    private readonly PullRequestCatalogue catalogue;

    public PullRequestCatalogueShould()
    {
        catalogue = new PullRequestCatalogue(new GitCli(processes), hosts, clock);
    }

    [Fact]
    public void AnswerLookingBeforeTheFirstListing()
    {
        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
    }

    [Fact]
    public async Task AnswerFromTheListingOnceItArrives()
    {
        await catalogue.RefreshStaleAsync([Checkout]);

        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Found(Open));
        catalogue.Find(Checkout, new BranchName("spike-cache")).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoPullRequest));
    }

    [Fact]
    public async Task CallOncePerRepositoryForManyBranches()
    {
        await catalogue.RefreshStaleAsync([Checkout, new FolderPath(@"c:\users\me\source\repos\Northwind.Orders\")]);

        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Found(Open));
        catalogue.Find(Checkout, new BranchName("spike-cache")).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoPullRequest));
        hosts.Calls.ShouldBe([Orders]);
    }

    [Fact]
    public async Task NotCallAgainWithinThreeMinutes()
    {
        await catalogue.RefreshStaleAsync([Checkout]);
        clock.Advance(TimeSpan.FromMinutes(3) - TimeSpan.FromSeconds(1));

        await catalogue.RefreshStaleAsync([Checkout]);

        hosts.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CallAgainAfterThreeMinutesKeepingTheOldAnswerMeanwhile()
    {
        await catalogue.RefreshStaleAsync([Checkout]);
        clock.Advance(TimeSpan.FromMinutes(3));
        hosts.Answer(Orders, Listing(Merged)).Hold();

        Task refresh = catalogue.RefreshStaleAsync([Checkout]);
        await WaitForCallsAsync(2);
        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Found(Open));
        hosts.Release();
        await refresh;

        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Found(Merged));
    }

    [Fact]
    public async Task NotStartASecondCallWhileOneRuns()
    {
        hosts.Hold();

        Task first = catalogue.RefreshStaleAsync([Checkout]);
        await WaitForCallsAsync(1);
        Task second = catalogue.RefreshStaleAsync([Checkout]);
        hosts.Release();
        await Task.WhenAll(first, second);

        hosts.Calls.Count.ShouldBe(1);
        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Found(Open));
    }

    [Fact]
    public async Task CacheAnUnavailableAnswerForTheWindow()
    {
        hosts.Answer(Orders, new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn));

        await catalogue.RefreshStaleAsync([Checkout]);
        await catalogue.RefreshStaleAsync([Checkout]);

        hosts.Calls.Count.ShouldBe(1);
        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NotSignedIn));
    }

    [Theory]
    [InlineData(2, "", PullRequestGap.NoRemote)]
    [InlineData(0, "https://gitlab.com/northwind/Northwind.Orders.git", PullRequestGap.UnknownHost)]
    public async Task AnswerNoRemoteAndUnknownHostWithoutCallingAHost(int exitCode, string url, PullRequestGap expected)
    {
        processes.AnswerAll(new ProcessResult(exitCode, url, exitCode == 0 ? string.Empty : "error: No such remote 'origin'"));

        await catalogue.RefreshStaleAsync([Checkout]);

        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Missing(expected));
        hosts.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerFailedWhenTheLookupThrows()
    {
        processes.Fail(new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified."));

        await catalogue.RefreshStaleAsync([Checkout]);

        catalogue.Find(Checkout, Fix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Failed));
    }

    [Fact]
    public async Task AnswerNoBranchForADetachedHead()
    {
        await catalogue.RefreshStaleAsync([Checkout]);

        catalogue.Find(Checkout, null).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
    }

    private static PullRequestListing.Listed Listing(PullRequest pullRequest) => new([new BranchPullRequest(Fix, pullRequest)]);

    private async Task WaitForCallsAsync(int count)
    {
        while (hosts.Calls.Count < count)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }
}