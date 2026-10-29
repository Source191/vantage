using Source191.Vantage.Adapters.PullRequests;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using System.ComponentModel;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class GitHubPullRequestsShould
{
    /// <summary>Recorded from gh 2.x's pr list --json, with the repository and branches made up.</summary>
    private const string Recorded = """
        [
          {"createdAt":"2026-10-20T09:12:00Z","headRefName":"fix-123","isDraft":false,"number":123,"state":"OPEN","url":"https://github.com/northwind/Northwind.Orders/pull/123"},
          {"createdAt":"2026-10-19T15:40:21Z","headRefName":"refund-retry","isDraft":true,"number":121,"state":"OPEN","url":"https://github.com/northwind/Northwind.Orders/pull/121"},
          {"createdAt":"2026-10-12T08:01:44Z","headRefName":"tidy-logging","isDraft":false,"number":88,"state":"MERGED","url":"https://github.com/northwind/Northwind.Orders/pull/88"},
          {"createdAt":"2026-10-02T11:30:00Z","headRefName":"spike-cache","isDraft":false,"number":80,"state":"CLOSED","url":"https://github.com/northwind/Northwind.Orders/pull/80"}
        ]
        """;

    private static readonly GitHubRepository Orders = new("northwind", "Northwind.Orders");
    private static readonly FolderPath Checkout = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, Recorded, string.Empty));

    private GitHubPullRequests Hosts => new(processes);

    [Fact]
    public async Task ListEveryBranchFromOneCall()
    {
        PullRequestListing listing = await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None);

        processes.CapturedRequests.Single().ShouldSatisfyAllConditions(
            request => request.FileName.ShouldBe("gh"),
            request => request.WorkingDirectory.ShouldBe(Checkout.Value),
            request => request.Arguments.ShouldBe(["pr", "list", "--repo", "northwind/Northwind.Orders", "--state", "all", "--limit", "100", "--json", "number,url,state,isDraft,headRefName,createdAt"]));
        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldBe(
        [
            Row("fix-123", 123, PullRequestState.Open, "2026-10-20T09:12:00Z"),
            Row("refund-retry", 121, PullRequestState.Draft, "2026-10-19T15:40:21Z"),
            Row("tidy-logging", 88, PullRequestState.Merged, "2026-10-12T08:01:44Z"),
            Row("spike-cache", 80, PullRequestState.Closed, "2026-10-02T11:30:00Z"),
        ]);
    }

    [Fact]
    public async Task ReportCliMissingWhenGhCannotStart()
    {
        processes.Fail(new Win32Exception(2, "The system cannot find the file specified."));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.CliMissing));
    }

    [Fact]
    public async Task ReportNotSignedInOnExitCodeFour()
    {
        processes.AnswerAll(new ProcessResult(4, string.Empty, "To get started with GitHub CLI, please run:  gh auth login"));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn));
    }

    [Theory]
    [InlineData(1, "GraphQL: Could not resolve to a Repository with the name 'northwind/Northwind.Orders'.")]
    [InlineData(0, "not json")]
    public async Task ReportFailedOnAnythingElse(int exitCode, string words)
    {
        processes.AnswerAll(new ProcessResult(exitCode, exitCode == 0 ? words : string.Empty, words));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.Failed));
    }

    private static BranchPullRequest Row(string branch, int number, PullRequestState state, string createdAt) =>
        new(BranchName.From(branch), new PullRequest(PullRequestHost.GitHub, number, new Uri($"https://github.com/northwind/Northwind.Orders/pull/{number}"), state, DateTimeOffset.Parse(createdAt, System.Globalization.CultureInfo.InvariantCulture)));

}