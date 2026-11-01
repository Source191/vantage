using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class PullRequestCatalogueShould : IDisposable
{
    private static readonly FolderPath Checkout = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");
    private static readonly GitHubRepository Orders = new("northwind", "Northwind.Orders");
    private static readonly BranchName Fix = BranchName.From("fix-123");
    private static readonly Head OnFix = new Head.OnBranch(Fix);
    private static readonly PullRequest Open = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero));
    private static readonly PullRequest Merged = Open with { State = PullRequestState.Merged };

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .Answer(request => request.Arguments.SequenceEqual(["remote", "get-url", "origin"]), new ProcessResult(0, "git@github.com:northwind/Northwind.Orders.git\n", string.Empty));

    private readonly FakePullRequests hosts = new FakePullRequests().Answer(Orders, Listing(Open));
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));
    private readonly DiagnosticLog log;
    private readonly RefreshCounts refreshes;
    private readonly PullRequestCatalogue catalogue;

    public PullRequestCatalogueShould()
    {
        log = new DiagnosticLog(clock, []);
        refreshes = new RefreshCounts(clock, log);
        catalogue = new PullRequestCatalogue(new GitCli(processes, "git"), hosts, clock, refreshes);
    }

    [Fact]
    public void AnswerLookingBeforeTheFirstListing()
    {
        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
    }

    [Fact]
    public async Task AnswerFromTheListingOnceItArrives()
    {
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Found(Open));
        catalogue.Find(Checkout, new Head.OnBranch(BranchName.From("spike-cache"))).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking), "a branch no row has asked for is not looked up");
    }

    [Fact]
    public async Task AskForOneBranchAtATime()
    {
        BranchName spike = BranchName.From("spike-cache");

        await catalogue.RefreshStaleAsync([(Checkout, Fix), (FolderPath.From(@"c:\users\me\source\repos\Northwind.Orders\"), spike)]);

        hosts.Requests.ShouldBe([(Orders, Fix), (Orders, spike)], ignoreOrder: true);
        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Found(Open));
        catalogue.Find(Checkout, new Head.OnBranch(spike)).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoPullRequest));
    }

    [Fact]
    public async Task KeepAListedAnswerForAMinute()
    {
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        clock.Advance(TimeSpan.FromSeconds(50));
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        hosts.Calls.Count.ShouldBe(1);

        clock.Advance(TimeSpan.FromSeconds(11));
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        hosts.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task KeepAnUnavailableAnswerForThreeMinutes()
    {
        hosts.Answer(Orders, new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn));
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        clock.Advance(TimeSpan.FromMinutes(2));
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        hosts.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CallAgainAfterAMinuteKeepingTheOldAnswerMeanwhile()
    {
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        clock.Advance(TimeSpan.FromSeconds(60));
        hosts.Answer(Orders, Listing(Merged)).Hold();

        Task refresh = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await WaitForCallsAsync(2);
        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Found(Open));
        hosts.Release();
        await refresh;

        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Found(Merged));
    }

    [Fact]
    public async Task RunAtMostFourLookupsAtOnce()
    {
        hosts.Hold();
        (FolderPath, BranchName)[] twelve = [.. Enumerable.Range(1, 12).Select(number => (Checkout, BranchName.From($"fix-{number}")))];

        Task refresh = catalogue.RefreshStaleAsync(twelve);
        await WaitForCallsAsync(4);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        hosts.Calls.Count.ShouldBe(4, "the rest wait for a free slot");
        hosts.Release();
        await refresh;

        hosts.Calls.Count.ShouldBe(12);
    }

    [Fact]
    public async Task NotStartASecondCallWhileOneRuns()
    {
        hosts.Hold();

        Task first = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await WaitForCallsAsync(1);
        Task second = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        hosts.Release();
        await Task.WhenAll(first, second);

        hosts.Calls.Count.ShouldBe(1);
        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Found(Open));
    }

    [Fact]
    public async Task CacheAnUnavailableAnswerForTheWindow()
    {
        hosts.Answer(Orders, new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn));

        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        hosts.Calls.Count.ShouldBe(1);
        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NotSignedIn, new LookupHost.Known(PullRequestHost.GitHub)), "the status line names the host's CLI");
    }

    [Theory]
    [InlineData(2, "", PullRequestGap.NoRemote)]
    [InlineData(0, "https://gitlab.com/northwind/Northwind.Orders.git", PullRequestGap.UnknownHost)]
    public async Task AnswerNoRemoteAndUnknownHostWithoutCallingAHost(int exitCode, string url, PullRequestGap expected)
    {
        processes.AnswerAll(new ProcessResult(exitCode, url, exitCode == 0 ? string.Empty : "error: No such remote 'origin'"));

        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Missing(expected, new LookupHost.NotRead()), "no host was read");
        hosts.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerFailedWhenTheLookupThrows()
    {
        processes.Fail(new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified."));

        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        catalogue.Find(Checkout, OnFix).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Failed, new LookupHost.NotRead()), "a lookup that failed read no host");
    }

    [Fact]
    public async Task AnswerNoBranchForADetachedOrUnknownHead()
    {
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);

        catalogue.Find(Checkout, new Head.Detached()).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
        catalogue.Find(Checkout, new Head.Unknown()).ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
    }

    [Fact]
    public async Task TimeARefreshThatStartsALookup()
    {
        hosts.Hold();

        Task refresh = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await WaitForCallsAsync(1);
        clock.Advance(TimeSpan.FromMilliseconds(1200));
        hosts.Release();
        await refresh;

        TimeSpan took = TimeSpan.FromMilliseconds(1200);
        refreshes.Snapshot().ShouldBe([new RefreshCount(RefreshPart.PullRequestRefresh, new DurationFigures(1, took, took, took))]);
    }

    [Fact]
    public async Task NotTimeARefreshWithNothingStale()
    {
        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        clock.Advance(TimeSpan.FromSeconds(10));

        await catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await catalogue.RefreshStaleAsync([]);

        refreshes.Snapshot().Single().Durations.Runs.ShouldBe(1, "only the first refresh started a lookup");
    }

    [Fact]
    public async Task NotTimeARefreshThatOnlyJoinsALookupAlreadyRunning()
    {
        hosts.Hold();
        Task first = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await WaitForCallsAsync(1);

        Task second = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        hosts.Release();
        await Task.WhenAll(first, second);

        refreshes.Snapshot().Single().Durations.Runs.ShouldBe(1);
    }

    [Fact]
    public async Task RecordARefreshSlowerThanThreeSeconds()
    {
        hosts.Hold();

        Task refresh = catalogue.RefreshStaleAsync([(Checkout, Fix)]);
        await WaitForCallsAsync(1);
        clock.Advance(TimeSpan.FromSeconds(4));
        hosts.Release();
        await refresh;

        log.Snapshot().Select(timed => timed.Event).ShouldBe([new RefreshSlow(RefreshPart.PullRequestRefresh, TimeSpan.FromSeconds(4))]);
    }

    private static PullRequestListing.Listed Listing(PullRequest pullRequest) => new([new BranchPullRequest(Fix, pullRequest)]);

    private async Task WaitForCallsAsync(int count)
    {
        while (hosts.Calls.Count < count)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    public void Dispose()
    {
        catalogue.Dispose();
    }
}