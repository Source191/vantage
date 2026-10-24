using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// Everything the launch wizard dialog needs from the rest of the system, behind one door so the dialog stays thin.
/// Starting the planned session is <see cref="Actions.StartSessionHandler"/>'s.
/// </summary>
public sealed class LaunchWorkflow(RepositoryCatalogue catalogue, IFolders folders, IRecentRepositories recent)
{
    /// <summary>The recently used repositories, read once when the wizard opens; filtering is then synchronous.</summary>
    public Task<IReadOnlyList<string>> RecentAsync() => recent.ReadRecentRepositoriesAsync();

    public IReadOnlyList<RepositoryChoice> Choices(IReadOnlyList<string> recent, string filter) => catalogue.Choices(recent, filter);

    public LaunchPlanResult Plan(LaunchRequest request) => LaunchPlanBuilder.Build(request, folders.DirectoryExists);
}