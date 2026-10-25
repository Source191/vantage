namespace Source191.Vantage.Domain;

/// <summary>The repository an origin remote points at, on a host Vantage can ask for pull requests.</summary>
public abstract record RemoteRepository
{
    public static RemoteRepository? Parse(string? url) => null;
}

/// <summary>A repository on github.com.</summary>
public sealed record GitHubRepository(string Owner, string Name) : RemoteRepository;

/// <summary>A repository in Azure DevOps.</summary>
public sealed record AzureDevOpsRepository(string Organisation, string Project, string Name) : RemoteRepository;
