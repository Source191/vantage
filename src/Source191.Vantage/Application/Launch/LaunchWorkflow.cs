using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// Everything the launch wizard dialog needs from the rest of the system, behind one door so the dialog stays thin.
/// </summary>
public sealed class LaunchWorkflow(RepositoryCatalogue catalogue, StateStore store, LaunchService service, TabOpener tabs, IWindowsTerminal terminal, string claudeExecutable)
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

    /// <summary>The footer after a successful start: the new session's job id and, when asked, the tab it was opened in.</summary>
    public async Task<string> StartedAsync(LaunchPlan plan, LaunchOutcome outcome, bool openInTab, CancellationToken cancellationToken)
    {
        string started = $"Started {plan.Name.Text} ({outcome.JobId}).";

        return (openInTab, outcome.Session) switch
        {
            (false, _) => started,
            (true, null) => $"{started} It is not listed yet: press o on it to open a tab.",
            (true, Session session) => $"{started} {await tabs.OpenAsync(session, cancellationToken)}",
        };
    }

    public async Task<string> OpenTrustTabAsync(string directory, CancellationToken cancellationToken)
    {
        await terminal.OpenTabAsync(TrustRecovery.TabFor(directory, claudeExecutable), cancellationToken);

        return TrustRecovery.Explanation(directory);
    }
}