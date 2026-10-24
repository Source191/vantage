using Source191.Vantage.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// The only state Vantage owns, under one folder: tab registrations, recent repositories and session descriptions
/// (one file each, so two Vantage processes writing at once cannot lose each other's records), worktrees Vantage
/// created, and config. A missing or corrupt file reads as empty. The single-file recent.json and sessions.json of
/// earlier versions are imported once and removed.
/// </summary>
public sealed class StateStore(string folder, TimeProvider clock)
{
    private const int RecentLimit = 10;

    // Enums are written as their names, so config.json keeps reading "RemoteDefault" and "Head".
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    private string TabsFolder => Path.Combine(folder, "tabs");

    private string RecentFolder => Path.Combine(folder, "recent");

    private string DescriptionsFolder => Path.Combine(folder, "descriptions");

    private string LegacyRecentFile => Path.Combine(folder, "recent.json");

    private string LegacySessionsFile => Path.Combine(folder, "sessions.json");

    private string CreatedFile => Path.Combine(folder, "created.json");

    private string ConfigFile => Path.Combine(folder, "config.json");

    public async Task<IReadOnlyList<TabRegistration>> ReadTabsAsync()
    {
        return await ReadAllAsync<TabRegistration>(TabsFolder);
    }

    public Task WriteTabAsync(TabRegistration registration)
    {
        return WriteJsonAsync(Path.Combine(TabsFolder, $"{registration.Id:N}.json"), registration);
    }

    public Task RemoveTabAsync(Guid id)
    {
        File.Delete(Path.Combine(TabsFolder, $"{id:N}.json"));

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

    public async Task<IReadOnlyList<CreatedWorktree>> ReadCreatedWorktreesAsync()
    {
        return await ReadJsonAsync<List<CreatedWorktree>>(CreatedFile) ?? [];
    }

    public async Task RecordCreatedWorktreeAsync(CreatedWorktree worktree)
    {
        IReadOnlyList<CreatedWorktree> existing = await ReadCreatedWorktreesAsync();

        await WriteJsonAsync(CreatedFile, existing.Where(created => !SamePath(created.Path, worktree.Path)).Append(worktree).ToList());
    }

    public async Task ForgetCreatedWorktreeAsync(string path)
    {
        IReadOnlyList<CreatedWorktree> existing = await ReadCreatedWorktreesAsync();

        await WriteJsonAsync(CreatedFile, existing.Where(created => !SamePath(created.Path, path)).ToList());
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadDescriptionsAsync()
    {
        await ImportLegacyDescriptionsAsync();
        IReadOnlyList<SessionDescription> descriptions = await ReadAllAsync<SessionDescription>(DescriptionsFolder);

        return descriptions.ToDictionary(description => description.SessionId, description => description.Description);
    }

    public async Task WriteDescriptionAsync(string sessionId, string description)
    {
        await ImportLegacyDescriptionsAsync();
        await WriteJsonAsync(DescriptionFile(sessionId), new SessionDescription(sessionId, description));
    }

    public async Task<VantageConfig> ReadConfigAsync()
    {
        return await ReadJsonAsync<VantageConfig>(ConfigFile) ?? VantageConfig.Default;
    }

    public Task WriteConfigAsync(VantageConfig config)
    {
        return WriteJsonAsync(ConfigFile, config);
    }

    private string RecentFile(string repository)
    {
        return Path.Combine(RecentFolder, $"{FileKey(repository.ToUpperInvariant())}.json");
    }

    private string DescriptionFile(string sessionId)
    {
        return Path.Combine(DescriptionsFolder, $"{FileKey(sessionId)}.json");
    }

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

        File.Delete(LegacyRecentFile);
    }

    private async Task ImportLegacyDescriptionsAsync()
    {
        Dictionary<string, string>? legacy = await ReadJsonAsync<Dictionary<string, string>>(LegacySessionsFile);
        if (legacy is null)
        {
            return;
        }

        foreach ((string sessionId, string description) in legacy)
        {
            await WriteJsonAsync(DescriptionFile(sessionId), new SessionDescription(sessionId, description));
        }

        File.Delete(LegacySessionsFile);
    }

    private async Task TrimRecentAsync()
    {
        IReadOnlyList<RecentRepository> recent = await ReadAllAsync<RecentRepository>(RecentFolder);
        foreach (RecentRepository stale in recent.OrderByDescending(entry => entry.UsedAt).Skip(RecentLimit))
        {
            File.Delete(RecentFile(stale.Path));
        }
    }

    private static string FileKey(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16].ToLowerInvariant();
    }

    private static bool SamePath(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

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

    private static async Task<T?> ReadJsonAsync<T>(string file)
        where T : class
    {
        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            await using FileStream stream = File.OpenRead(file);

            return await JsonSerializer.DeserializeAsync<T>(stream, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteJsonAsync<T>(string file, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await using FileStream stream = File.Create(file);
        await JsonSerializer.SerializeAsync(stream, value, Options);
    }

    private sealed record RecentRepository(string Path, DateTimeOffset UsedAt);

    private sealed record SessionDescription(string SessionId, string Description);
}