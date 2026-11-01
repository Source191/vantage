using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The tabs Vantage has opened, one record each, read with stale ones still present (the caller judges liveness).</summary>
public interface ITabRegistry
{
    Task<IReadOnlyList<TabRegistration>> ReadTabsAsync();

    Task WriteTabAsync(TabRegistration registration);

    Task RemoveTabAsync(Guid id);
}

/// <summary>The repositories sessions were last launched in, most recent first, capped at ten.</summary>
public interface IRecentRepositories
{
    Task<IReadOnlyList<string>> ReadRecentRepositoriesAsync();

    Task RecordRecentRepositoryAsync(string repository);
}

/// <summary>The worktrees and branches Vantage created, which decide the branch-deletion rules.</summary>
public interface ICreatedWorktrees
{
    Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync();

    Task RecordCreatedWorktreeAsync(CreatedWorktree worktree);

    Task ForgetCreatedWorktreeAsync(FolderPath path);
}

/// <summary>User configuration (config.json): <see cref="VantageConfig.Missing"/> when there is no file or it cannot be read.</summary>
public interface IVantageConfig
{
    Task<VantageConfig> ReadConfigAsync();

    Task WriteConfigAsync(VantageConfig config);
}

/// <summary>The last update check: never made (or a record that cannot be read), or when Vantage last asked for its newest release and what it found.</summary>
public abstract record UpdateCheckRecord
{
    public sealed record Never : UpdateCheckRecord;

    public sealed record Checked(DateTimeOffset CheckedAt, NewestRelease Newest) : UpdateCheckRecord;
}

/// <summary>The last update check, so Vantage asks at most once a day; only a check that was made is written.</summary>
public interface IUpdateCheckRecords
{
    Task<UpdateCheckRecord> ReadUpdateCheckAsync();

    Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record);
}

/// <summary>Copies the gitignored files a repository's .worktreeinclude names into a new worktree.</summary>
public interface IWorktreeIncludeCopier
{
    Task<IReadOnlyList<string>> CopyAsync(string repository, string worktree, CancellationToken cancellationToken);
}