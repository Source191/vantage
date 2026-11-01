using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// The only state Vantage owns, under one folder: tab registrations, recent repositories
/// (one file each, so two Vantage processes writing at once cannot lose each other's records), worktrees Vantage
/// created, config, the last update check and the last version run. A missing or corrupt file reads as empty. The single-file recent.json of earlier
/// versions is imported once and removed; the descriptions\ folder and sessions.json of earlier versions are no longer read.
/// </summary>
public sealed class JsonFileStore(string folder, TimeProvider clock) : ITabRegistry, IRecentRepositories, ICreatedWorktrees, IVantageConfig, IUpdateCheckRecords, ILastRunVersion
{
    private const int RecentLimit = 10;

    // Enums are written as their names, so config.json keeps reading "RemoteDefault" and "Head"; paths, branch names
    // and tab owners are written as the plain strings earlier versions wrote.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new TextValueJsonConverter<FolderPath>(), new TextValueJsonConverter<BranchName>(), new TabOwnerJsonConverter() },
    };

    private string TabsFolder => Path.Combine(folder, "tabs");

    private string RecentFolder => Path.Combine(folder, "recent");

    private string LegacyRecentFile => Path.Combine(folder, "recent.json");

    private string CreatedFile => Path.Combine(folder, "created.json");

    /// <summary>The settings file, which the Config tab names.</summary>
    public string ConfigFile => Path.Combine(folder, "config.json");

    private string UpdateCheckPath => Path.Combine(folder, "update-check.json");

    private string LastRunPath => Path.Combine(folder, "last-run.json");

    public async Task<IReadOnlyList<TabRegistration>> ReadTabsAsync() => [.. (await ReadAllAsync<TabFile>(TabsFolder)).Select(file => file.ToRegistration())];

    public Task WriteTabAsync(TabRegistration registration) => WriteJsonAsync(Path.Combine(TabsFolder, $"{registration.Id:N}.json"), TabFile.From(registration));

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

    public async Task<VantageConfig> ReadConfigAsync() => await ReadJsonAsync<VantageConfig>(ConfigFile) ?? VantageConfig.Missing;

    public Task WriteConfigAsync(VantageConfig config) => WriteJsonAsync(ConfigFile, config);

    public async Task<UpdateCheckRecord> ReadUpdateCheckAsync()
    {
        UpdateCheckFile? file = await ReadJsonAsync<UpdateCheckFile>(UpdateCheckPath);

        return file is null ? new UpdateCheckRecord.Never() : new UpdateCheckRecord.Checked(file.CheckedAt, NewestOf(file.Newest), NotesOf(file.Notes));
    }

    public Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record) => WriteJsonAsync(UpdateCheckPath, new UpdateCheckFile(record.CheckedAt, RecordedText(record.Newest), RecordedNotes(record.NewestNotes)));

    public async Task<LastRun> ReadLastRunAsync()
    {
        LastRunFile? file = await ReadJsonAsync<LastRunFile>(LastRunPath);

        return ReleaseVersion.TryParse(file?.Version, out ReleaseVersion? version) ? new LastRun.Ran(version) : new LastRun.Never();
    }

    public Task WriteLastRunAsync(ReleaseVersion version) => WriteJsonAsync(LastRunPath, new LastRunFile(version.ToString()));

    /// <summary>The notes as the file keeps them, the text read from nuget.org; text that is not a changelog, or none, is no notes.</summary>
    private static ReleaseNotes NotesOf(string? text) => ReleaseNotes.TryParse(text, out ReleaseNotes? notes) ? notes : ReleaseNotes.None;

    /// <summary>No notes are left out of the file, which then reads as every earlier version wrote it.</summary>
    private static string? RecordedNotes(ReleaseNotes notes) => notes.Text.Length == 0 ? null : notes.Text;
    /// <summary>The newest release the file names: a version as Found, any other text as Unreadable, and no text as None.</summary>
    private static NewestRelease NewestOf(string? text) => text is string recorded ? NewestRelease.Recorded(recorded) : new NewestRelease.None();

    /// <summary>The file keeps the newest release as text, and no text when there is none.</summary>
    private static string? RecordedText(NewestRelease newest)
    {
        return newest switch
        {
            NewestRelease.Found found => found.Version.ToString(),
            NewestRelease.None => null,
            NewestRelease.Unreadable unreadable => unreadable.Text,
            _ => throw new UnreachableException(),
        };
    }

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
    /// Writes one record beside its file and moves it over, so a crash, a kill or a full disc leaves the previous file
    /// and no reader sees a half-written one. Readers open with delete sharing, which is what lets the move replace a
    /// file another Vantage process has open. The temporary name is unique because a tab file has two writers.
    /// </summary>
    private static async Task WriteJsonAsync<T>(string file, T value)
    {
        if (Path.GetDirectoryName(file) is string folder)
        {
            Directory.CreateDirectory(folder);
        }

        string temporary = $"{file}.{Guid.NewGuid():N}.tmp";
        await using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, value, Options);
        }

        ReplaceOrMove(temporary, file);
    }

    /// <summary>
    /// File.Move with overwrite is refused over a file another process holds open even with delete sharing, where
    /// File.Replace is not, so an existing file is replaced and only a missing one is moved into place.
    /// </summary>
    private static void ReplaceOrMove(string temporary, string file)
    {
        try
        {
            File.Replace(temporary, file, destinationBackupFileName: null);
        }
        catch (FileNotFoundException)
        {
            File.Move(temporary, file);
        }
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

    /// <summary>update-check.json as written, <c>{"checkedAt": ..., "newest": "0.10.0"}</c> or <c>"newest": null</c>, and <c>"notes"</c> (the newest release's notes as read) only when there are some, so without them it is the same file every earlier version wrote.</summary>
    private sealed record UpdateCheckFile(DateTimeOffset CheckedAt, string? Newest, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Notes = null);

    /// <summary>last-run.json, <c>{"version": "0.21.0"}</c>: the released version that last ran.</summary>
    private sealed record LastRunFile(string? Version);
}