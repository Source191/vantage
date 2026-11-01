using Source191.Vantage.Adapters.PullRequests;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using System.ComponentModel;
using System.Globalization;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class AzureDevOpsPullRequestsShould
{
    private const string Az = @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";
    private const string WebUrl = "https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders";

    /// <summary>
    /// In the shape az repos pr list --output json really prints, trimmed to the fields read, with the organisation made
    /// up: the repository object carries no webUrl, which is why the link is built from the remote.
    /// </summary>
    private const string Recorded = """
        [
          {"creationDate":"2026-10-20T09:12:00.1234567+00:00","isDraft":false,"pullRequestId":4521,"repository":{"name":"Northwind.Orders","url":"https://dev.azure.com/northwind/0f1c/_apis/git/repositories/9a2b"},"sourceRefName":"refs/heads/fix-123","status":"active"},
          {"creationDate":"2026-10-19T15:40:21+00:00","isDraft":true,"pullRequestId":4519,"repository":{"name":"Northwind.Orders","url":"https://dev.azure.com/northwind/0f1c/_apis/git/repositories/9a2b"},"sourceRefName":"refs/heads/feature/refund-retry","status":"active"},
          {"creationDate":"2026-10-12T08:01:44+00:00","isDraft":false,"pullRequestId":4460,"repository":{"name":"Northwind.Orders","url":"https://dev.azure.com/northwind/0f1c/_apis/git/repositories/9a2b"},"sourceRefName":"refs/heads/tidy-logging","status":"completed"},
          {"creationDate":"2026-10-02T11:30:00+00:00","isDraft":false,"pullRequestId":4401,"repository":{"name":"Northwind.Orders","url":"https://dev.azure.com/northwind/0f1c/_apis/git/repositories/9a2b"},"sourceRefName":"refs/heads/spike-cache","status":"abandoned"}
        ]
        """;

    private static readonly AzureDevOpsRepository Orders = new("northwind", "Northwind Trading", "Northwind.Orders");
    private static readonly FolderPath Checkout = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, Recorded, string.Empty));

    private AzureDevOpsPullRequests Hosts => new(processes, new ProgramLocation.Found(Az));

    [Fact]
    public async Task AskForTheBranchOnly()
    {
        PullRequestListing listing = await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None);

        processes.CapturedRequests.Single().ShouldSatisfyAllConditions(
            request => request.FileName.ShouldBe(Az),
            request => request.WorkingDirectory.ShouldBe(Checkout.Value),
            request => request.Arguments.ShouldBe(["repos", "pr", "list", "--org", "https://dev.azure.com/northwind", "--project", "Northwind Trading", "--repository", "Northwind.Orders", "--source-branch", "fix-123", "--status", "all", "--top", "20", "--output", "json"]));
        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldBe(
        [
            Row("fix-123", 4521, PullRequestState.Active, "2026-10-20T09:12:00.1234567+00:00"),
            Row("feature/refund-retry", 4519, PullRequestState.Draft, "2026-10-19T15:40:21+00:00"),
            Row("tidy-logging", 4460, PullRequestState.Completed, "2026-10-12T08:01:44+00:00"),
            Row("spike-cache", 4401, PullRequestState.Abandoned, "2026-10-02T11:30:00+00:00"),
        ]);
    }

    [Theory]
    [InlineData("feature/x&calc")]
    [InlineData("say\"hi\"")]
    [InlineData("100%done")]
    [InlineData("a|b")]
    public async Task NotStartAzForABranchItCannotBeGiven(string branch)
    {
        PullRequestListing listing = await Hosts.ListAsync(Orders, Checkout, BranchName.From(branch), CancellationToken.None);

        listing.ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.BranchNotGivenToAz));
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepABranchWithASlash()
    {
        BranchName republisher = BranchName.From("feature/republisher");
        processes.AnswerAll(new ProcessResult(0, """
            [{"creationDate":"2026-10-21T10:00:00+00:00","isDraft":true,"pullRequestId":4530,"repository":{"name":"Northwind.Orders"},"sourceRefName":"refs/heads/feature/republisher","status":"active"}]
            """, string.Empty));

        PullRequestListing listing = await Hosts.ListAsync(Orders, Checkout, republisher, CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldContain("feature/republisher");
        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.Single().Branch.ShouldBe(republisher);
    }

    [Fact]
    public async Task BuildTheLinkFromTheRemote()
    {
        PullRequestListing listing = await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None);

        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests[0].PullRequest.Url
            .ShouldBe(new Uri("https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521"));
    }

    [Fact]
    public async Task AnswerCliMissingWhenAzIsMissing()
    {
        AzureDevOpsPullRequests hosts = new(processes, new ProgramLocation.Missing());

        (await hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.CliMissing));
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportCliMissingWhenAzCannotStart()
    {
        processes.Fail(new Win32Exception(2, "The system cannot find the file specified."));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.CliMissing));
    }

    [Theory]
    [InlineData("ERROR: 'repos' is misspelled or not recognized by the system.")]
    [InlineData("The command requires the extension azure-devops. Unable to prompt for extension install confirmation as no tty available.")]
    public async Task ReportCliMissingWithoutTheExtension(string words)
    {
        processes.AnswerAll(new ProcessResult(2, string.Empty, words));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.CliMissing));
    }

    [Theory]
    [InlineData("ERROR: Please run 'az login' to setup account.")]
    [InlineData("ERROR: Before you can run Azure DevOps commands, you need to run the login command (az login if using AAD/MSA identity else az devops login if using PAT token) to setup credentials.")]
    public async Task ReportNotSignedInOnTheLoginHint(string words)
    {
        processes.AnswerAll(new ProcessResult(1, string.Empty, words));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn));
    }

    [Theory]
    [InlineData(1, "ERROR: TF401019: The Git repository with name or identifier Northwind.Orders does not exist.")]
    [InlineData(0, "not json")]
    public async Task ReportFailedOnAnythingElse(int exitCode, string words)
    {
        processes.AnswerAll(new ProcessResult(exitCode, exitCode == 0 ? words : string.Empty, words));

        (await Hosts.ListAsync(Orders, Checkout, BranchName.From("fix-123"), CancellationToken.None)).ShouldBe(new PullRequestListing.Unavailable(PullRequestGap.Failed));
    }

    private static BranchPullRequest Row(string branch, int number, PullRequestState state, string createdAt) =>
        new(BranchName.From(branch), new PullRequest(PullRequestHost.AzureDevOps, number, new Uri($"{WebUrl}/pullrequest/{number}"), state, DateTimeOffset.Parse(createdAt, CultureInfo.InvariantCulture)));
}