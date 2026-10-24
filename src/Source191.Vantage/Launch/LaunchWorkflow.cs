using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;

namespace Source191.Vantage.Launch;

/// <summary>
/// Everything the launch wizard dialog needs from the rest of the system, behind one door so the dialog stays thin.
/// </summary>
public sealed class LaunchWorkflow(RepositoryCatalogue catalogue, StateStore store, LaunchService service, IWindowsTerminal terminal, string claudeExecutable)
{
    /// <summary>The recently used repositories, read once when the wizard opens; filtering is then synchronous.</summary>
    public Task<IReadOnlyList<string>> RecentAsync()
    {
        return store.ReadRecentRepositoriesAsync();
    }

    public IReadOnlyList<RepositoryChoice> Choices(IReadOnlyList<string> recent, string filter)
    {
        return catalogue.Choices(recent, filter);
    }

    public static LaunchPlanResult Plan(LaunchRequest request)
    {
        return LaunchPlanBuilder.Build(request, Directory.Exists);
    }

    public Task<LaunchOutcome> LaunchAsync(LaunchPlan plan, CancellationToken cancellationToken)
    {
        return service.LaunchAsync(plan, cancellationToken);
    }

    public async Task<string> OpenTrustTabAsync(string directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(TrustRecovery.TabFor(directory, claudeExecutable), cancellationToken);

        return TrustRecovery.Explanation(directory);
    }
}