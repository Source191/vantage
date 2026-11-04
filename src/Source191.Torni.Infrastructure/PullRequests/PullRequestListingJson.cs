using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Text.Json;

namespace Source191.Torni.Adapters.PullRequests;

/// <summary>
/// Walks a host's JSON array of pull requests, one row at a time. A row the adapter leaves out (null) or that lacks a
/// property it reads is skipped, as a link that is not https is; text that is not the expected JSON fails the listing.
/// </summary>
internal static class PullRequestListingJson
{
    public static PullRequestListing Rows(string json, Func<JsonElement, BranchPullRequest?> row)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            List<BranchPullRequest> rows = [.. document.RootElement.EnumerateArray().Select(item => Skipping(item, row)).OfType<BranchPullRequest>()];

            return new PullRequestListing.Listed(rows);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.Failed);
        }
    }

    private static BranchPullRequest? Skipping(JsonElement item, Func<JsonElement, BranchPullRequest?> row)
    {
        try
        {
            return row(item);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }
}