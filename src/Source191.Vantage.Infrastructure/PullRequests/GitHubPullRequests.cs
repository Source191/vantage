using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>Lists a GitHub repository's pull requests with gh pr list.</summary>
public sealed class GitHubPullRequests(IProcessRunner processes)
{
    public async Task<PullRequestListing> ListAsync(GitHubRepository repository, FolderPath checkout, CancellationToken cancellationToken)
    {
        await processes.RunAsync(new ProcessRequest("gh", [], checkout.Value), cancellationToken);
        return new PullRequestListing.Unavailable(PullRequestGap.Looking);
    }
}
