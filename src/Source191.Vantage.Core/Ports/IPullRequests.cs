using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>
/// The boundary to the hosts' CLIs (gh for GitHub, az for Azure DevOps). One call lists the pull requests of one
/// branch of a repository; it never throws for a missing or signed-out CLI, answering Unavailable instead.
/// </summary>
public interface IPullRequests
{
    Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken);
}