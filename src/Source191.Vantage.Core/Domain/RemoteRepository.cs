using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>
/// The repository an origin remote points at, on a host Vantage can ask for pull requests: github.com or Azure DevOps.
/// Any other remote, and an Azure DevOps name that is not plain text for a batch file (<see cref="BatchFileArgument"/>),
/// is not read: az is a .cmd file that cmd.exe parses, so its arguments must hold none of its special characters.
/// </summary>
public abstract partial record RemoteRepository
{
    /// <summary>The host whose CLI lists this repository's pull requests.</summary>
    public abstract PullRequestHost Host { get; }

    public static bool TryParse(string? url, [MaybeNullWhen(false)] out RemoteRepository repository)
    {
        string text = url?.Trim() ?? string.Empty;
        Match gitHub = GitHub().Match(text);
        if (gitHub.Success)
        {
            repository = new GitHubRepository(gitHub.Groups["owner"].Value, gitHub.Groups["name"].Value);
            return true;
        }

        if (AzureDevOps().Match(text) is { Success: true } azure && Azure(azure, out AzureDevOpsRepository? found))
        {
            repository = found;
            return true;
        }

        repository = null;
        return false;
    }

    private static bool Azure(Match match, [MaybeNullWhen(false)] out AzureDevOpsRepository repository)
    {
        string organisation = Uri.UnescapeDataString(match.Groups["org"].Value);
        string project = Uri.UnescapeDataString(match.Groups["project"].Value);
        string name = Uri.UnescapeDataString(match.Groups["name"].Value);

        string[] parts = [organisation, project, name];

        if (parts.All(BatchFileArgument.IsSafe))
        {
            repository = new AzureDevOpsRepository(organisation, project, name);
            return true;
        }

        repository = null;
        return false;
    }

    [GeneratedRegex(@"^(?:https://github\.com/|git@github\.com:|ssh://git@github\.com/)(?<owner>[^/\s]+)/(?<name>[^/\s]+?)(?:\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex GitHub();

    [GeneratedRegex(@"^(?:https://(?:[^@/\s]+@)?dev\.azure\.com/(?<org>[^/\s]+)/(?<project>[^/\s]+)/_git/(?<name>[^/\s]+?)" +
        @"|https://(?:[^@/\s]+@)?(?<org>[^./\s]+)\.visualstudio\.com/(?:DefaultCollection/)?(?<project>[^/\s]+)/_git/(?<name>[^/\s]+?)" +
        @"|git@ssh\.dev\.azure\.com:v3/(?<org>[^/\s]+)/(?<project>[^/\s]+)/(?<name>[^/\s]+?)" +
        @"|[^@/\s]+@vs-ssh\.visualstudio\.com:v3/(?<org>[^/\s]+)/(?<project>[^/\s]+)/(?<name>[^/\s]+?))/?$", RegexOptions.IgnoreCase)]
    private static partial Regex AzureDevOps();
}

/// <summary>A repository on github.com.</summary>
public sealed record GitHubRepository(string Owner, string Name) : RemoteRepository
{
    public override PullRequestHost Host => PullRequestHost.GitHub;
}

/// <summary>A repository in Azure DevOps.</summary>
public sealed record AzureDevOpsRepository(string Organisation, string Project, string Name) : RemoteRepository
{
    public override PullRequestHost Host => PullRequestHost.AzureDevOps;
}