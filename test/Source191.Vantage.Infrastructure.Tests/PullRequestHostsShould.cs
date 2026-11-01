using Source191.Vantage.Adapters.PullRequests;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class PullRequestHostsShould : IDisposable
{
    private const string Az = @"C:\Users\me\az\az.cmd";

    private static readonly FolderPath Checkout = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, "[]", string.Empty));
    private readonly string folder = Path.Combine(Path.GetTempPath(), "vantage-az-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("gh")]
    [InlineData(Az)]
    public async Task SendEachRemoteToItsHost(string expected)
    {
        RemoteRepository repository = expected == "gh" ? new GitHubRepository("northwind", "Northwind.Orders") : new AzureDevOpsRepository("northwind", "Trading", "Northwind.Orders");
        PullRequestHosts hosts = new(new GitHubPullRequests(processes, "gh"), new AzureDevOpsPullRequests(processes, Az));

        PullRequestListing listing = await hosts.ListAsync(repository, Checkout, BranchName.From("fix-123"), CancellationToken.None);

        processes.CapturedRequests.Single().FileName.ShouldBe(expected);
        listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldBeEmpty();
    }

    [Fact]
    public void LocateAzCmdOnThePathAsAProgramLocation()
    {
        string bin = Path.Combine(folder, "wbin");
        string later = Path.Combine(folder, "later");
        Directory.CreateDirectory(bin);
        Directory.CreateDirectory(later);
        File.WriteAllText(Path.Combine(bin, "az.cmd"), "@echo off");
        File.WriteAllText(Path.Combine(later, "az.cmd"), "@echo off");

        AzureCliExecutable.Locate($"{Path.Combine(folder, "missing")};{bin};{later}").ShouldBe(new ProgramLocation.Found(Path.Combine(bin, "az.cmd")), "the first PATH folder holding az.cmd wins");
        AzureCliExecutable.Locate(Path.Combine(folder, "missing")).ShouldBe(new ProgramLocation.Missing());
        AzureCliExecutable.Locate(null).ShouldBe(new ProgramLocation.Missing());
    }

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}