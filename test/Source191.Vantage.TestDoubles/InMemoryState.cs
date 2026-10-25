using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>
/// Every state port in memory, with the same rules as the file store: recent repositories most recent first and
/// capped at ten, one created-worktree record per path.
/// </summary>
public sealed class InMemoryState : ITabRegistry, IRecentRepositories, ICreatedWorktrees, IVantageConfig, IUpdateCheckRecords
{
    private const int RecentLimit = 10;

    private readonly Dictionary<Guid, TabRegistration> tabs = [];
    private readonly List<string> recent = [];
    private readonly List<CreatedWorktree> created = [];
    private UpdateCheckRecord? updateCheck;
    private VantageConfig? config;

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
        created.RemoveAll(existing => existing.Path == worktree.Path);
        created.Add(worktree);
        return Task.CompletedTask;
    }

    public Task ForgetCreatedWorktreeAsync(FolderPath path)
    {
        created.RemoveAll(existing => existing.Path == path);
        return Task.CompletedTask;
    }

    public Task<VantageConfig?> ReadConfigAsync()
    {
        return Task.FromResult(config);
    }

    public Task WriteConfigAsync(VantageConfig config)
    {
        this.config = config;
        return Task.CompletedTask;
    }

    public Task<UpdateCheckRecord?> ReadUpdateCheckAsync()
    {
        return Task.FromResult(updateCheck);
    }

    public Task WriteUpdateCheckAsync(UpdateCheckRecord record)
    {
        updateCheck = record;
        return Task.CompletedTask;
    }
}