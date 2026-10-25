using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// The only state Vantage owns, under one folder: tab registrations, recent repositories
/// (one file each, so two Vantage processes writing at once cannot lose each other's records), worktrees Vantage
/// created, config, and the last update check. A missing or corrupt file reads as empty. The single-file recent.json of earlier
/// versions is imported once and removed; the descriptions\ folder and sessions.json of earlier versions are no longer read.
/// </summary>
public sealed class JsonFileStore(string folder, TimeProvider clock) : ITabRegistry, IRecentRepositories, ICreatedWorktrees, IVantageConfig, IUpdateCheckRecords
{
    private const int RecentLimit = 10;

    // Enums are written as their names, so config.json keeps reading "RemoteDefault" and "Head"; paths, branch names
    // and tab owners are written as the plain strings earlier versions wrote.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new TextValueJsonConverter<FolderPath>(), new TextValueJsonConverter<BranchName>(), new TabOwnerJsonConverter() },
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver { Modifiers = { StateFileNames.KeepTabOwnerAsSessionId } },
    };

    private string TabsFolder => Path.Combine(folder, "tabs");

    private string RecentFolder => Path.Combine(folder, "recent");

    private string LegacyRecentFile => Path.Combine(folder, "recent.json");

    private string CreatedFile => Path.Combine(folder, "created.json");

    private string ConfigFile => Path.Combine(folder, "config.json");

    private string UpdateCheckFile => Path.Combine(folder, "update-check.json");

    public async Task<IReadOnlyList<TabRegistration>> ReadTabsAsync() => await ReadAllAsync<TabRegistration>(TabsFolder);

    public Task WriteTabAsync(TabRegistration registration) => WriteJsonAsync(Path.Combine(TabsFolder, $"{registration.Id:N}.json"), registration);

    public Task RemoveTabAsync(Guid id)
    {
        TryDelete(Path.Combine(TabsFolder, $"{id:N}.json"));

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> ReadRecentRepositoriesAsync()
    {
        await ImportLegacyRecentAsync();
        IReadOnlyList<RecentRepository> recent = await ReadAllAsync<RecentRepository>(RecentFolder);

        return [.. recent.OrderByDescending(entry => entry.UsedAt).Take(RecentLimit).Select(entry => entry.Path)];
    }

    public async Task RecordRecentRepositoryAsync(string repository)
    {
        await ImportLegacyRecentAsync();
        await WriteJsonAsync(RecentFile(repository), new RecentRepository(repository, clock.GetUtcNow()));
        await TrimRecentAsync();
    }

    public async Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync() => await ReadJsonAsync<List<CreatedWorktree>>(CreatedFile) ?? [];

    public async Task RecordCreatedWorktreeAsync(CreatedWorktree worktree)
    {
        IReadOnlyList<CreatedWorktree> existing = await ReadCreatedWorktreesAsync();

        await WriteJsonAsync(CreatedFile, existing.Where(created => created.Path != worktree.Path).Append(worktree).ToList());
    }

    public async Task ForgetCreatedWorktreeAsync(FolderPath path)
    {
        IReadOnlyList<CreatedWorktree> existing = await ReadCreatedWorktreesAsync();

        await WriteJsonAsync(CreatedFile, existing.Where(created => created.Path != path).ToList());
    }

    public Task<VantageConfig?> ReadConfigAsync() => ReadJsonAsync<VantageConfig>(ConfigFile);

    public Task WriteConfigAsync(VantageConfig config) => WriteJsonAsync(ConfigFile, config);

    public Task<UpdateCheckRecord?> ReadUpdateCheckAsync() => ReadJsonAsync<UpdateCheckRecord>(UpdateCheckFile);

    public Task WriteUpdateCheckAsync(UpdateCheckRecord record) => WriteJsonAsync(UpdateCheckFile, record);

    private string RecentFile(string repository) => Path.Combine(RecentFolder, $"{FileKey(repository.ToUpperInvariant())}.json");

    /// <summary>The old single list, most recent first, becomes one file each with descending timestamps so the order survives.</summary>
    private async Task ImportLegacyRecentAsync()
    {
        List<string>? legacy = await ReadJsonAsync<List<string>>(LegacyRecentFile);
        if (legacy is null)
        {
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        foreach ((string repository, int index) in legacy.Select((repository, index) => (repository, index)))
        {
            await WriteJsonAsync(RecentFile(repository), new RecentRepository(repository, now.AddSeconds(-index)));
        }

        TryDelete(LegacyRecentFile);
    }

    private async Task TrimRecentAsync()
    {
        IReadOnlyList<RecentRepository> recent = await ReadAllAsync<RecentRepository>(RecentFolder);
        foreach (RecentRepository stale in recent.OrderByDescending(entry => entry.UsedAt).Skip(RecentLimit))
        {
            TryDelete(RecentFile(stale.Path));
        }
    }

    private static string FileKey(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16].ToLowerInvariant();

    private static async Task<IReadOnlyList<T>> ReadAllAsync<T>(string directory)
        where T : class
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<T> items = [];
        foreach (string file in Directory.EnumerateFiles(directory, "*.json"))
        {
            T? item = await ReadJsonAsync<T>(file);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    /// Reads one record. Another Vantage process may be replacing or deleting the file at the same moment, so the
    /// stream allows sharing and a file that is locked, gone or half-written reads as absent rather than failing the
    /// whole read; the next refresh sees the finished file.
    /// </summary>
    private static async Task<T?> ReadJsonAsync<T>(string file)
        where T : class
    {
        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            await using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            return await JsonSerializer.DeserializeAsync<T>(stream, Options);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes one record, allowing readers in while it does: Windows refuses to rename over a file another process
    /// has open, so an atomic replace is not available, and a reader that catches the record half-written treats it as
    /// absent until the next refresh instead.
    /// </summary>
    private static async Task WriteJsonAsync<T>(string file, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await using FileStream stream = new(file, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, value, Options);
    }

    /// <summary>A file another process is reading or has already removed is not an error; it will be gone soon enough.</summary>
    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (IOException)
        {
            // Left for the next read or write to clear.
        }
    }

    private sealed record RecentRepository(string Path, DateTimeOffset UsedAt);
}