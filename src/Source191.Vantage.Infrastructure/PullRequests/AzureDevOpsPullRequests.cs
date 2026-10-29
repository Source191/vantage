using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>
/// Lists one branch's pull requests in an Azure DevOps repository with az repos pr list, run through the full path of az.cmd (null
/// when it is not on the PATH). The only place az's words are read: its login hints are not signed in, a missing
/// azure-devops extension is a missing CLI, anything else failed. Every argument is a fixed flag or a name the Domain
/// has already checked, so cmd.exe, which reads az.cmd's arguments, finds nothing special in them.
/// </summary>
public sealed class AzureDevOpsPullRequests(IProcessRunner processes, string? az)
{
    private const string BranchPrefix = "refs/heads/";

    public async Task<PullRequestListing> ListAsync(AzureDevOpsRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken)
    {
        if (az is null)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.CliMissing);
        }

        ProcessRequest request = new(az, ["repos", "pr", "list", "--org", $"https://dev.azure.com/{repository.Organisation}", "--project", repository.Project, "--repository", repository.Name, "--source-branch", branch.Value, "--status", "all", "--top", "20", "--output", "json"], checkout.Value);
        ProcessResult result;
        try
        {
            result = await processes.RunAsync(request, cancellationToken);
        }
        catch (Win32Exception)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.CliMissing);
        }

        return result.ExitCode == 0 ? Parse(result.StandardOutput, repository) : new PullRequestListing.Unavailable(Gap(result.StandardError));
    }

    private static PullRequestGap Gap(string words) => words switch
    {
        _ when words.Contains("az login", StringComparison.Ordinal) || words.Contains("az devops login", StringComparison.Ordinal) => PullRequestGap.NotSignedIn,
        _ when words.Contains("azure-devops", StringComparison.Ordinal) || words.Contains("'repos' is misspelled", StringComparison.Ordinal) => PullRequestGap.CliMissing,
        _ => PullRequestGap.Failed,
    };

    private static PullRequestListing Parse(string json, AzureDevOpsRepository repository)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            List<BranchPullRequest> rows = [];
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (Row(item, repository) is BranchPullRequest row)
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

    /// <summary>The page is built from the remote's organisation, project and repository: az's listing carries no web URL.</summary>
    private static BranchPullRequest? Row(JsonElement item, AzureDevOpsRepository repository)
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