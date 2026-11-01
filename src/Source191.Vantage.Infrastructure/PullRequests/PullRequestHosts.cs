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

/// <summary>
/// Finds az.cmd on the PATH;
/// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">CreateProcess
/// cannot find a .cmd by its bare name</see>.
/// </summary>
public static class AzureCliExecutable
{
    /// <summary>The first az.cmd in the PATH's folders, in order; the variable itself may be unset.</summary>
    public static ProgramLocation Locate(string? path)
    {
        string? found = (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Path.Combine(folder, "az.cmd"))
            .FirstOrDefault(File.Exists);

        return found is null ? new ProgramLocation.Missing() : new ProgramLocation.Found(found);
    }
}