using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// The only state Vantage owns, under one folder: tab registrations (one file each), recent repositories,
/// worktrees Vantage created, and session descriptions. A missing or corrupt file reads as empty.
/// </summary>
public sealed class StateStore(string folder)
{
    private const int RecentLimit = 10;

    // Enums are written as their names, so config.json keeps reading "RemoteDefault" and "Head".
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    private string TabsFolder => Path.Combine(folder, "tabs");

    private string RecentFile => Path.Combine(folder, "recent.json");

    private string CreatedFile => Path.Combine(folder, "created.json");

    private string SessionsFile => Path.Combine(folder, "sessions.json");

    public async Task<IReadOnlyList<TabRegistration>> ReadTabsAsync()
    {
        if (!Directory.Exists(TabsFolder))
        {
            return [];
        }

        List<TabRegistration> registrations = [];
        foreach (string file in Directory.EnumerateFiles(TabsFolder, "*.json"))
        {
            TabRegistration? registration = await ReadJsonAsync<TabRegistration>(file);
            if (registration is not null)
            {
                registrations.Add(registration);
            }
        }

        return registrations;
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
        return await ReadJsonAsync<List<string>>(RecentFile) ?? [];
    }

    public async Task RecordRecentRepositoryAsync(string repository)
    {
        IReadOnlyList<string> existing = await ReadRecentRepositoriesAsync();
        List<string> updated = [repository, .. existing.Where(path => !string.Equals(path, repository, StringComparison.OrdinalIgnoreCase))];

        await WriteJsonAsync(RecentFile, updated.Take(RecentLimit).ToList());
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
        return await ReadJsonAsync<Dictionary<string, string>>(SessionsFile) ?? [];
    }

    public async Task WriteDescriptionAsync(string sessionId, string description)
    {
        Dictionary<string, string> descriptions = new(await ReadDescriptionsAsync())
        {
            [sessionId] = description,
        };

        await WriteJsonAsync(SessionsFile, descriptions);
    }

    public async Task<VantageConfig> ReadConfigAsync()
    {
        return await ReadJsonAsync<VantageConfig>(ConfigFile) ?? VantageConfig.Default;
    }

    public Task WriteConfigAsync(VantageConfig config)
    {
        return WriteJsonAsync(ConfigFile, config);
    }

    private string ConfigFile => Path.Combine(folder, "config.json");

    private static bool SamePath(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
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
}