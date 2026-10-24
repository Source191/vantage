using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.UnitTests.TestDoubles;

/// <summary>
/// Every state port in memory, with the same rules as the file store: recent repositories most recent first and
/// capped at ten, one created-worktree record per path, descriptions keyed by session id.
/// </summary>
public sealed class InMemoryState : ITabRegistry, IRecentRepositories, ICreatedWorktrees, ISessionDescriptions, IVantageConfig
{
    private const int RecentLimit = 10;

    private readonly Dictionary<Guid, TabRegistration> tabs = [];
    private readonly List<string> recent = [];
    private readonly List<CreatedWorktree> created = [];
    private readonly Dictionary<string, string> descriptions = [];
    private VantageConfig config = VantageConfig.Default;

    public Task<IReadOnlyList<TabRegistration>> ReadTabsAsync()
    {
        return Task.FromResult<IReadOnlyList<TabRegistration>>([.. tabs.Values]);
    }

    public Task WriteTabAsync(TabRegistration registration)
    {
        tabs[registration.Id] = registration;
        return Task.CompletedTask;
    }

    public Task RemoveTabAsync(Guid id)
    {
        tabs.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ReadRecentRepositoriesAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>([.. recent]);
    }

    public Task RecordRecentRepositoryAsync(string repository)
    {
        recent.RemoveAll(path => string.Equals(path, repository, StringComparison.OrdinalIgnoreCase));
        recent.Insert(0, repository);
        if (recent.Count > RecentLimit)
        {
            recent.RemoveRange(RecentLimit, recent.Count - RecentLimit);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync()
    {
        return Task.FromResult<IReadOnlyList<CreatedWorktree>>([.. created]);
    }

    public Task RecordCreatedWorktreeAsync(CreatedWorktree worktree)
    {
        created.RemoveAll(existing => string.Equals(existing.Path, worktree.Path, StringComparison.OrdinalIgnoreCase));
        created.Add(worktree);
        return Task.CompletedTask;
    }

    public Task ForgetCreatedWorktreeAsync(string path)
    {
        created.RemoveAll(existing => string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string>> ReadDescriptionsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(descriptions));
    }

    public Task WriteDescriptionAsync(string sessionId, string description)
    {
        descriptions[sessionId] = description;
        return Task.CompletedTask;
    }

    public Task<VantageConfig> ReadConfigAsync()
    {
        return Task.FromResult(config);
    }

    public Task WriteConfigAsync(VantageConfig config)
    {
        this.config = config;
        return Task.CompletedTask;
    }
}