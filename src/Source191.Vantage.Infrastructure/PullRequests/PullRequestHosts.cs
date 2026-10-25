using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>Sends each repository to the adapter for its host.</summary>
public sealed class PullRequestHosts(GitHubPullRequests gitHub, AzureDevOpsPullRequests azureDevOps) : IPullRequests
{
    public Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, CancellationToken cancellationToken) => repository switch
    {
        GitHubRepository github => gitHub.ListAsync(github, checkout, cancellationToken),
        AzureDevOpsRepository azure => azureDevOps.ListAsync(azure, checkout, cancellationToken),
        _ => Task.FromResult<PullRequestListing>(new PullRequestListing.Unavailable(PullRequestGap.UnknownHost)),
    };
}

/// <summary>Finds az.cmd on the PATH; CreateProcess cannot find a .cmd by its bare name.</summary>
public static class AzureCliExecutable
{
    public static string? Locate(string? path) =>
        (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Path.Combine(folder, "az.cmd"))
            .FirstOrDefault(File.Exists);
}