using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

/// <summary>
/// Every state port in memory, with the same rules as the file store: recent repositories most recent first and
/// capped at ten, one created-worktree record per path.
/// </summary>
public sealed class InMemoryState : ITabRegistry, IRecentRepositories, ICreatedWorktrees, ITorniConfig, IUpdateCheckRecords, ILastRunVersion
{
    private const int RecentLimit = 10;

    private readonly Lock gate = new();
    private readonly Dictionary<Guid, TabRegistration> tabs = [];
    private readonly List<FolderPath> recent = [];
    private readonly List<CreatedWorktree> created = [];
    private UpdateCheckRecord updateCheck = new UpdateCheckRecord.Never();
    private TorniConfig config = TorniConfig.Missing;
    private LastRun lastRun = new LastRun.Never();
    private Exception? nextRecordFailure;

    public Task<IReadOnlyList<TabRegistration>> ReadTabsAsync()
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<TabRegistration>>([.. tabs.Values]);
        }
    }

    public Task WriteTabAsync(TabRegistration registration)
    {
        lock (gate)
        {
            tabs[registration.Id] = registration;
        }

        return Task.CompletedTask;
    }

    public Task RemoveTabAsync(Guid id)
    {
        lock (gate)
        {
            tabs.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FolderPath>> ReadRecentRepositoriesAsync()
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<FolderPath>>([.. recent]);
        }
    }

    public Task RecordRecentRepositoryAsync(FolderPath repository)
    {
        lock (gate)
        {
            recent.RemoveAll(path => path.Equals(repository));
            recent.Insert(0, repository);
            if (recent.Count > RecentLimit)
            {
                recent.RemoveRange(RecentLimit, recent.Count - RecentLimit);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync()
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<CreatedWorktree>>([.. created]);
        }
    }

    /// <summary>Makes the next <c>RecordCreatedWorktreeAsync</c> throw the exception, as a state file the file system refuses would.</summary>
    public void FailNextCreatedWorktreeRecord(Exception exception)
    {
        nextRecordFailure = exception;
    }

    public Task RecordCreatedWorktreeAsync(CreatedWorktree worktree)
    {
        if (nextRecordFailure is Exception failure)
        {
            nextRecordFailure = null;
            return Task.FromException(failure);
        }

        lock (gate)
        {
            created.RemoveAll(existing => existing.Path == worktree.Path);
            created.Add(worktree);
        }

        return Task.CompletedTask;
    }

    public Task ForgetCreatedWorktreeAsync(FolderPath path)
    {
        lock (gate)
        {
            created.RemoveAll(existing => existing.Path == path);
        }

        return Task.CompletedTask;
    }

    public Task<TorniConfig> ReadConfigAsync()
    {
        return Task.FromResult(config);
    }

    public Task WriteConfigAsync(TorniConfig config)
    {
        this.config = config;
        return Task.CompletedTask;
    }

    public Task<UpdateCheckRecord> ReadUpdateCheckAsync()
    {
        return Task.FromResult(updateCheck);
    }

    public Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record)
    {
        updateCheck = record;
        return Task.CompletedTask;
    }

    public Task<LastRun> ReadLastRunAsync()
    {
        return Task.FromResult(lastRun);
    }

    public Task WriteLastRunAsync(ReleaseVersion version)
    {
        lastRun = new LastRun.Ran(version);
        return Task.CompletedTask;
    }
}