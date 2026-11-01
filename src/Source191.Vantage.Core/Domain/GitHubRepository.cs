namespace Source191.Vantage.Domain;

/// <summary>A repository on github.com.</summary>
public sealed record GitHubRepository(string Owner, string Name) : RemoteRepository
{
    public override PullRequestHost Host => PullRequestHost.GitHub;
}