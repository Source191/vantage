using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>
/// Lists a GitHub repository's pull requests with gh pr list. The only place gh's words are read: exit code 4 (and its
/// gh auth login hint) is not signed in, a gh that cannot start is missing, anything else failed.
/// </summary>
public sealed class GitHubPullRequests(IProcessRunner processes)
{
    private const int NotSignedInExitCode = 4;

    public async Task<PullRequestListing> ListAsync(GitHubRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken)
    {
        ProcessRequest request = new("gh", ["pr", "list", "--repo", $"{repository.Owner}/{repository.Name}", "--head", branch.Value, "--state", "all", "--limit", "20", "--json", "number,url,state,isDraft,headRefName,createdAt"], checkout.Value);
        ProcessResult result;
        try
        {
            result = await processes.RunAsync(request, cancellationToken);
        }
        catch (Win32Exception)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.CliMissing);
        }

        return result switch
        {
            { ExitCode: 0 } => Parse(result.StandardOutput),
            { ExitCode: NotSignedInExitCode } => new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn),
            _ when result.StandardError.Contains("gh auth login", StringComparison.Ordinal) => new PullRequestListing.Unavailable(PullRequestGap.NotSignedIn),
            _ => new PullRequestListing.Unavailable(PullRequestGap.Failed),
        };
    }

    private static PullRequestListing Parse(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            List<BranchPullRequest> rows = [];
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (Row(item) is BranchPullRequest row)
                {
                    rows.Add(row);
                }
            }

            return new PullRequestListing.Listed(rows);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.Failed);
        }
    }

    private static BranchPullRequest? Row(JsonElement item)
    {
        PullRequestState? state = State(item.GetProperty("state").GetString(), item.GetProperty("isDraft").GetBoolean());
        if (state is null || !BranchName.TryParse(item.GetProperty("headRefName").GetString(), out BranchName? branch) || !Uri.TryCreate(item.GetProperty("url").GetString(), UriKind.Absolute, out Uri? url))
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