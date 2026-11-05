using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Adapters.PullRequests;

/// <summary>Sends each repository to the adapter for its host.</summary>
public sealed class PullRequestHosts(GitHubPullRequests gitHub, AzureDevOpsPullRequests azureDevOps) : IPullRequests
{
    public Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken) => repository switch
    {
        RemoteRepository.GitHub github => gitHub.ListAsync(github, checkout, branch, cancellationToken),
        RemoteRepository.AzureDevOps azure => azureDevOps.ListAsync(azure, checkout, branch, cancellationToken),
        _ => Task.FromResult<PullRequestListing>(new PullRequestListing.Unavailable(PullRequestGap.UnknownHost)),
    };
}