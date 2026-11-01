using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>Sends each repository to the adapter for its host.</summary>
public sealed class PullRequestHosts(GitHubPullRequests gitHub, AzureDevOpsPullRequests azureDevOps) : IPullRequests
{
    public Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken) => repository switch
    {
        GitHubRepository github => gitHub.ListAsync(github, checkout, branch, cancellationToken),
        AzureDevOpsRepository azure => azureDevOps.ListAsync(azure, checkout, branch, cancellationToken),
        _ => Task.FromResult<PullRequestListing>(new PullRequestListing.Unavailable(PullRequestGap.UnknownHost)),
    };
}