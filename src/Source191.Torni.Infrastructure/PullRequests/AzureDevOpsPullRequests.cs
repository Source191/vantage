using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Source191.Torni.Adapters.PullRequests;

/// <summary>
/// Lists one branch's pull requests in an Azure DevOps repository with
/// <see href="https://learn.microsoft.com/cli/azure/repos/pr">az repos pr list</see>, run through the full path of az.cmd
/// (missing when it is not on the PATH). The only place az's words are read: its login hints are not signed in, a missing
/// <see href="https://learn.microsoft.com/azure/devops/cli/">azure-devops extension</see> is a missing CLI, anything else failed. Every argument is a fixed flag, a name the Domain
/// has already checked, or the branch, checked here by the same rule, so cmd.exe, which reads az.cmd's arguments,
/// finds nothing special in them; a branch that fails the rule is not looked up at all.
/// </summary>
public sealed class AzureDevOpsPullRequests(IProcessRunner processes, ProgramLocation az)
{
    private const string BranchPrefix = "refs/heads/";

    public Task<PullRequestListing> ListAsync(RemoteRepository.AzureDevOps repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken) => az switch
    {
        ProgramLocation.Found found => ListAsync(found.Path, repository, checkout, branch, cancellationToken),
        ProgramLocation.Missing => Task.FromResult<PullRequestListing>(new PullRequestListing.Unavailable(PullRequestGap.CliMissing)),
        _ => throw new UnreachableException(),
    };

    private async Task<PullRequestListing> ListAsync(string executable, RemoteRepository.AzureDevOps repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken)
    {
        if (!BatchFileArgument.IsSafe(branch.Value))
        {
            return new PullRequestListing.Unavailable(PullRequestGap.BranchNotGivenToAz);
        }

        ProcessRequest request = new(executable, ["repos", "pr", "list", "--org", $"https://dev.azure.com/{repository.Organisation}", "--project", repository.Project, "--repository", repository.Name, "--source-branch", branch.Value, "--status", "all", "--top", "20", "--output", "json"], new WorkingFolder.At(checkout.Value));
        return await CliListing.RunAsync(processes, request, result => result.ExitCode == 0 ? PullRequestListingJson.Rows(result.StandardOutput, item => Row(item, repository)) : new PullRequestListing.Unavailable(Gap(result.StandardError)), cancellationToken);
    }

    private static PullRequestGap Gap(string words) => words switch
    {
        _ when words.Contains("az login", StringComparison.Ordinal) || words.Contains("az devops login", StringComparison.Ordinal) => PullRequestGap.NotSignedIn,
        _ when words.Contains("azure-devops", StringComparison.Ordinal) || words.Contains("'repos' is misspelled", StringComparison.Ordinal) => PullRequestGap.CliMissing,
        _ => PullRequestGap.Failed,
    };

    /// <summary>The page is built from the remote's organisation, project and repository: az's listing carries no web URL.</summary>
    private static BranchPullRequest? Row(JsonElement item, RemoteRepository.AzureDevOps repository)
    {
        int number = item.GetProperty("pullRequestId").GetInt32();
        string source = item.GetProperty("sourceRefName").GetString() ?? string.Empty;
        string page = $"https://dev.azure.com/{Uri.EscapeDataString(repository.Organisation)}/{Uri.EscapeDataString(repository.Project)}/_git/{Uri.EscapeDataString(repository.Name)}/pullrequest/{number}";
        PullRequestState? state = State(item.GetProperty("status").GetString(), item.GetProperty("isDraft").GetBoolean());
        if (state is null
            || !source.StartsWith(BranchPrefix, StringComparison.Ordinal)
            || !BranchName.TryParse(source[BranchPrefix.Length..], out BranchName? branch)
            || !Uri.TryCreate(page, UriKind.Absolute, out Uri? url))
        {
            return null;
        }

        DateTimeOffset createdAt = DateTimeOffset.Parse(item.GetProperty("creationDate").GetString() ?? string.Empty, CultureInfo.InvariantCulture);

        return new BranchPullRequest(branch, new PullRequest(PullRequestHost.AzureDevOps, number, url, state.Value, createdAt));
    }

    private static PullRequestState? State(string? status, bool isDraft) => status switch
    {
        "active" when isDraft => PullRequestState.Draft,
        "active" => PullRequestState.Active,
        "completed" => PullRequestState.Completed,
        "abandoned" => PullRequestState.Abandoned,
        _ => null,
    };
}