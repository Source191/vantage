using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>One branch's pull request in a host's listing.</summary>
public sealed record BranchPullRequest(BranchName Branch, PullRequest PullRequest);

/// <summary>A host's answer for one repository: every pull request it listed, or why it could not list them.</summary>
public abstract record PullRequestListing
{
    public sealed record Listed(IReadOnlyList<BranchPullRequest> PullRequests) : PullRequestListing;

    public sealed record Unavailable(PullRequestGap Gap) : PullRequestListing;
}

/// <summary>
/// The boundary to the hosts' CLIs (gh for GitHub, az for Azure DevOps). One call lists a repository's recent pull
/// requests for every branch; it never throws for a missing or signed-out CLI, answering Unavailable instead.
/// </summary>
public interface IPullRequests
{
    Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, CancellationToken cancellationToken);
}
