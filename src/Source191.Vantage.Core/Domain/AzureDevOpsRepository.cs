namespace Source191.Vantage.Domain;

/// <summary>A repository in Azure DevOps.</summary>
public sealed record AzureDevOpsRepository(string Organisation, string Project, string Name) : RemoteRepository
{
    public override PullRequestHost Host => PullRequestHost.AzureDevOps;
}