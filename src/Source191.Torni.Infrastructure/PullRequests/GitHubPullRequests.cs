using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Source191.Torni.Adapters.PullRequests;

/// <summary>
/// Lists one branch's pull requests in a GitHub repository with
/// <see href="https://cli.github.com/manual/gh_pr_list">gh pr list</see>, run by the full path of gh.exe (missing when
/// it is not on the PATH, which reads as the CLI missing). The only place gh's words are read:
/// <see href="https://cli.github.com/manual/gh_help_exit-codes">exit code 4</see> (and its gh auth login hint) is not
/// signed in, a gh that cannot start is missing, anything else failed.
/// </summary>
public sealed class GitHubPullRequests(IProcessRunner processes, ProgramLocation gh)
{
    private const int NotSignedInExitCode = 4;

    public Task<PullRequestListing> ListAsync(RemoteRepository.GitHub repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken) => gh switch
    {
        ProgramLocation.Found found => ListAsync(found.Path, repository, checkout, branch, cancellationToken),
        ProgramLocation.Missing => Task.FromResult<PullRequestListing>(new PullRequestListing.Unavailable(PullRequestGap.CliMissing)),
        _ => throw new UnreachableException(),
    };

    private Task<PullRequestListing> ListAsync(string executable, RemoteRepository.GitHub repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken)
    {
        ProcessRequest request = new(executable, ["pr", "list", "--repo", $"{repository.Owner}/{repository.Name}", "--head", branch.Value, "--state", "all", "--limit", "20", "--json", "number,url,state,isDraft,headRefName,createdAt"], new WorkingFolder.At(checkout.Value));

        return CliListing.RunAsync(processes, request, Read, cancellationToken);
    }

    private static PullRequestListing Read(ProcessResult result) => result switch
    {
        { ExitCode: 0 } => PullRequestListingJson.Rows(result.StandardOutput, Row),
        { ExitCode: NotSignedInExitCode } => new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn),
        _ when result.StandardError.Contains("gh auth login", StringComparison.Ordinal) => new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn),
        _ => new PullRequestListing.Unavailable(PullRequestGap.Failed),
    };

    /// <summary>A row whose link is not https is left out: the link is handed to the shell to open, so only a web page is accepted.</summary>
    private static BranchPullRequest? Row(JsonElement item)
    {
        PullRequestState? state = State(item.GetProperty("state").GetString(), item.GetProperty("isDraft").GetBoolean());
        if (state is null
            || !BranchName.TryParse(item.GetProperty("headRefName").GetString(), out BranchName? branch)
            || !Uri.TryCreate(item.GetProperty("url").GetString(), UriKind.Absolute, out Uri? url)
            || url.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        DateTimeOffset createdAt = DateTimeOffset.Parse(item.GetProperty("createdAt").GetString() ?? string.Empty, CultureInfo.InvariantCulture);

        return new BranchPullRequest(branch, new PullRequest(PullRequestHost.GitHub, item.GetProperty("number").GetInt32(), url, state.Value, createdAt));
    }

    private static PullRequestState? State(string? state, bool isDraft) => state switch
    {
        "OPEN" when isDraft => PullRequestState.Draft,
        "OPEN" => PullRequestState.Open,
        "MERGED" => PullRequestState.Merged,
        "CLOSED" => PullRequestState.Closed,
        _ => null,
    };
}