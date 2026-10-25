using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class JsonFileStoreShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid SessionTabId = Guid.Parse("0b8d2a1c-9e3f-4b5a-8c7d-6e5f4a3b2c1d");
    private static readonly Guid OwnTabId = Guid.Parse("1c9e3f4b-5a8c-7d6e-5f4a-3b2c1d0b8d2a");

    private static readonly TabRegistration SessionTab = new(SessionTabId, new TabOwner.ForSession(SessionId.From("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3")), TabPalette.Default[2], Noon, 4242, Noon.AddSeconds(5));
    private static readonly TabRegistration OwnTab = new(OwnTabId, TabOwner.Vantage, TabPalette.Default[4], Noon, 5151, Noon.AddSeconds(1));

    /// <summary>A session's tab file exactly as the store wrote it before ids were typed: the owner is the session id string.</summary>
    private const string SessionTabJson = """
        {
          "id": "0b8d2a1c-9e3f-4b5a-8c7d-6e5f4a3b2c1d",
          "sessionId": "1a4d2d86-8e87-44fe-a9f7-61d47d481ae3",
          "colour": {
            "name": "Yellow",
            "hex": "#FFB900"
          },
          "createdAt": "2026-10-23T12:00:00+00:00",
          "processId": 4242,
          "processStartedAt": "2026-10-23T12:00:05+00:00"
        }
        """;

    /// <summary>Vantage's own tab file exactly as the store wrote it before ids were typed: the owner is the word vantage.</summary>
    private const string OwnTabJson = """
        {
          "id": "1c9e3f4b-5a8c-7d6e-5f4a-3b2c1d0b8d2a",
          "sessionId": "vantage",
          "colour": {
            "name": "Green",
            "hex": "#16C60C"
          },
          "createdAt": "2026-10-23T12:00:00+00:00",
          "processId": 5151,
          "processStartedAt": "2026-10-23T12:00:01+00:00"
        }
        """;

    /// <summary>created.json exactly as the store wrote it before paths became FolderPath: plain strings.</summary>
    private const string CreatedJson = """
        [
          {
            "path": "C:\\Users\\me\\source\\repos\\Northwind.Orders-fix-123",
            "branch": "fix-123",
            "repository": "C:\\Users\\me\\source\\repos\\Northwind.Orders",
            "createdAt": "2026-10-23T12:00:00+00:00"
          }
        ]
        """;

    private readonly string folder = Path.Combine(Path.GetTempPath(), "vantage-state-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider clock = new(Noon);

    private JsonFileStore Store => new(folder, clock);

    [Fact]
    public async Task StartEmptyWhenTheFolderDoesNotExist()
    {
        (await Store.ReadTabsAsync()).ShouldBeEmpty();
        (await Store.ReadRecentRepositoriesAsync()).ShouldBeEmpty();
        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RoundTripATabRegistration()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default[2], Noon);
        TabRegistration claimed = pending.ClaimedBy(4242, Noon.AddSeconds(5));

        await Store.WriteTabAsync(claimed);

        (await Store.ReadTabsAsync()).Single().ShouldBe(claimed);
    }

    [Fact]
    public async Task ReadTabFilesWrittenByTheCurrentVersion()
    {
        Directory.CreateDirectory(Path.Combine(folder, "tabs"));
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs", "0b8d2a1c9e3f4b5a8c7d6e5f4a3b2c1d.json"), SessionTabJson, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs", "1c9e3f4b5a8c7d6e5f4a3b2c1d0b8d2a.json"), OwnTabJson, TestContext.Current.CancellationToken);

        IReadOnlyList<TabRegistration> tabs = await Store.ReadTabsAsync();

        tabs.Single(tab => tab.Id == SessionTabId).ShouldBe(SessionTab);
        tabs.Single(tab => tab.Id == OwnTabId).ShouldBe(OwnTab);
    }

    [Fact]
    public async Task WriteTabFilesTheCurrentVersionCanRead()
    {
        await Store.WriteTabAsync(SessionTab);
        await Store.WriteTabAsync(OwnTab);

        (await File.ReadAllTextAsync(Path.Combine(folder, "tabs", $"{SessionTabId:N}.json"), TestContext.Current.CancellationToken)).ReplaceLineEndings("\n").ShouldBe(SessionTabJson.ReplaceLineEndings("\n"));
        (await File.ReadAllTextAsync(Path.Combine(folder, "tabs", $"{OwnTabId:N}.json"), TestContext.Current.CancellationToken)).ReplaceLineEndings("\n").ShouldBe(OwnTabJson.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task RemoveATabRegistration()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default[2], Noon);
        await Store.WriteTabAsync(pending);

        await Store.RemoveTabAsync(pending.Id);

        (await Store.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task IgnoreACorruptTabFile()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default[2], Noon);
        await Store.WriteTabAsync(pending);
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs", "broken.json"), "{ not json", TestContext.Current.CancellationToken);

        (await Store.ReadTabsAsync()).Single().ShouldBe(pending);
    }

    [Fact]
    public async Task KeepRecentRepositoriesMostRecentFirstWithoutDuplicates()
    {
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        clock.Advance(TimeSpan.FromSeconds(1));
        await Store.RecordRecentRepositoryAsync(@"C:\repos\B");
        clock.Advance(TimeSpan.FromSeconds(1));
        await Store.RecordRecentRepositoryAsync(@"C:\repos\a");

        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\a", @"C:\repos\B"]);
    }

    [Fact]
    public async Task KeepOnlyTheTenMostRecentRepositories()
    {
        foreach (int index in Enumerable.Range(1, 12))
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Store.RecordRecentRepositoryAsync($@"C:\repos\R{index}");
        }

        IReadOnlyList<string> recent = await Store.ReadRecentRepositoriesAsync();

        recent.Count.ShouldBe(10);
        recent[0].ShouldBe(@"C:\repos\R12");
        Directory.EnumerateFiles(Path.Combine(folder, "recent")).Count().ShouldBe(10, "the two oldest records are deleted");
    }

    [Fact]
    public async Task KeepRecentRepositoriesWrittenByTwoWriters()
    {
        JsonFileStore console = Store;
        JsonFileStore here = new(folder, new FakeTimeProvider(Noon.AddSeconds(1)));

        await Task.WhenAll(console.RecordRecentRepositoryAsync(@"C:\repos\A"), here.RecordRecentRepositoryAsync(@"C:\repos\B"));

        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\B", @"C:\repos\A"]);
    }

    [Fact]
    public async Task SkipARecordAnotherProcessIsStillWritingInsteadOfFailingTheRead()
    {
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        string lockedFile = Path.Combine(folder, "recent", "being-written.json");
        await using FileStream exclusive = new(lockedFile, FileMode.Create, FileAccess.Write, FileShare.None);

        IReadOnlyList<string> recent = await Store.ReadRecentRepositoriesAsync();
        await Store.RecordRecentRepositoryAsync(@"C:\repos\B");

        recent.ShouldBe([@"C:\repos\A"]);
    }

    [Fact]
    public async Task WriteARecordWhileAnotherProcessHasItOpenForReading()
    {
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        string file = Directory.EnumerateFiles(Path.Combine(folder, "recent")).Single();
        await using FileStream reading = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        clock.Advance(TimeSpan.FromSeconds(1));

        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        await Store.RecordRecentRepositoryAsync(@"C:\repos\B");

        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\B", @"C:\repos\A"]);
    }

    [Fact]
    public async Task TreatAHalfWrittenRecordAsAbsentUntilItIsFinished()
    {
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        await File.WriteAllTextAsync(Path.Combine(folder, "recent", "half.json"), """{ "path": "C:\\repos\\Half", "usedA""", TestContext.Current.CancellationToken);

        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\A"]);
    }

    [Fact]
    public async Task ImportTheLegacyRecentFile()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "recent.json"), """["C:\\repos\\Newest", "C:\\repos\\Older"]""", TestContext.Current.CancellationToken);

        IReadOnlyList<string> recent = await Store.ReadRecentRepositoriesAsync();
        recent.ShouldBe([@"C:\repos\Newest", @"C:\repos\Older"]);
        File.Exists(Path.Combine(folder, "recent.json")).ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveOldDescriptionFilesUntouched()
    {
        Directory.CreateDirectory(Path.Combine(folder, "descriptions"));
        string descriptionFile = Path.Combine(folder, "descriptions", "old.json");
        string sessionsFile = Path.Combine(folder, "sessions.json");
        await File.WriteAllTextAsync(descriptionFile, """{ "sessionId": "session-1", "description": "Replace the login page" }""", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(sessionsFile, """{ "session-1": "Replace the login page" }""", TestContext.Current.CancellationToken);
        byte[] descriptionBytes = await File.ReadAllBytesAsync(descriptionFile, TestContext.Current.CancellationToken);
        byte[] sessionsBytes = await File.ReadAllBytesAsync(sessionsFile, TestContext.Current.CancellationToken);

        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        await Store.ReadRecentRepositoriesAsync();
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon));
        await Store.ReadCreatedWorktreesAsync();

        (await File.ReadAllBytesAsync(descriptionFile, TestContext.Current.CancellationToken)).ShouldBe(descriptionBytes);
        (await File.ReadAllBytesAsync(sessionsFile, TestContext.Current.CancellationToken)).ShouldBe(sessionsBytes);
    }

    [Fact]
    public async Task RoundTripCreatedWorktreesAndForgetOne()
    {
        CreatedWorktree first = new(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon);
        CreatedWorktree second = new(FolderPath.From(@"C:\repos\B-feat"), BranchName.From("feat"), FolderPath.From(@"C:\repos\B"), Noon);
        await Store.RecordCreatedWorktreeAsync(first);
        await Store.RecordCreatedWorktreeAsync(second);

        await Store.ForgetCreatedWorktreeAsync(first.Path);

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([second]);
    }

    [Fact]
    public async Task ReadFilesWrittenByTheCurrentVersion()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "created.json"), CreatedJson, TestContext.Current.CancellationToken);

        CreatedWorktree created = (await Store.ReadCreatedWorktreesAsync()).Single();

        created.ShouldBe(new CreatedWorktree(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123"), BranchName.From("fix-123"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon));
    }

    [Fact]
    public async Task WriteFilesTheCurrentVersionCanRead()
    {
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123"), BranchName.From("fix-123"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon));

        string written = await File.ReadAllTextAsync(Path.Combine(folder, "created.json"), TestContext.Current.CancellationToken);

        written.ReplaceLineEndings("\n").ShouldBe(CreatedJson.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task TreatACreatedFileHoldingARelativePathAsCorrupt()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "created.json"), CreatedJson.Replace(@"C:\\Users\\me\\source\\repos\\Northwind.Orders-fix-123", @"Northwind.Orders-fix-123", StringComparison.Ordinal), TestContext.Current.CancellationToken);

        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadNoConfigurationWhenNoneIsSaved()
    {
        (await Store.ReadConfigAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task RoundTripTheConfiguration()
    {
        VantageConfig saved = new([@"C:\repos", @"D:\work"], WorktreeBase.Head, @"D:\worktrees", new EditorSetting("Custom", @"C:\Tools\subl.exe", ["-n", "{folder}"]));

        await Store.WriteConfigAsync(saved);

        (await Store.ReadConfigAsync()).ShouldBe(saved);
    }

    [Fact]
    public async Task ReadAnEarlierConfigWithoutTheNewProperties()
    {
        Directory.CreateDirectory(folder);
        string configFile = Path.Combine(folder, "config.json");
        await File.WriteAllTextAsync(configFile, """{ "repositoryRoots": ["D:\\work"], "defaultBase": "Head" }""", TestContext.Current.CancellationToken);

        VantageConfig read = (await Store.ReadConfigAsync())!;

        read.ShouldBe(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null));
    }

    [Fact]
    public async Task WriteCheckForUpdates()
    {
        VantageConfig saved = new([@"C:\repos"], WorktreeBase.RemoteDefault, @"C:\worktrees", new EditorSetting("VsCode", Executable: null, Arguments: null), CheckForUpdates: false);

        await Store.WriteConfigAsync(saved);

        (await Store.ReadConfigAsync()).ShouldBe(saved);
        (await File.ReadAllTextAsync(Path.Combine(folder, "config.json"), TestContext.Current.CancellationToken)).ShouldContain("\"checkForUpdates\": false");
    }

    [Fact]
    public async Task ReadAConfigWithoutABaseAsHavingNone()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), """{ "repositoryRoots": ["D:\\work"] }""", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync())!.DefaultBase.ShouldBeNull();
    }

    [Fact]
    public async Task ReadAndWriteTheWorktreeBaseAsAnEnum()
    {
        Directory.CreateDirectory(folder);
        string configFile = Path.Combine(folder, "config.json");
        await File.WriteAllTextAsync(configFile, """{ "repositoryRoots": ["C:\\repos"], "defaultBase": "Head" }""", TestContext.Current.CancellationToken);

        VantageConfig read = (await Store.ReadConfigAsync())!;
        await Store.WriteConfigAsync(read);

        read.DefaultBase.ShouldBe(WorktreeBase.Head);
        (await File.ReadAllTextAsync(configFile, TestContext.Current.CancellationToken)).ShouldContain("\"Head\"");
    }

    [Fact]
    public async Task WriteTheEditorAsAPresetObject()
    {
        await Store.WriteConfigAsync(new VantageConfig([@"C:\repos"], WorktreeBase.RemoteDefault, @"C:\worktrees", new EditorSetting("VsCode", Executable: null, Arguments: null)));

        string written = await File.ReadAllTextAsync(Path.Combine(folder, "config.json"), TestContext.Current.CancellationToken);

        written.ShouldContain("\"repositoryRoots\"");
        written.ShouldContain("\"worktreeRoot\": \"C:\\\\worktrees\"");
        written.ShouldContain("\"defaultBase\": \"RemoteDefault\"");
        written.ShouldContain("\"editor\": {");
        written.ShouldContain("\"preset\": \"VsCode\"");
    }

    [Fact]
    public async Task WriteACustomEditorWithItsArgumentList()
    {
        await Store.WriteConfigAsync(new VantageConfig([@"C:\repos"], WorktreeBase.RemoteDefault, @"C:\worktrees", new EditorSetting("Custom", @"C:\Tools\subl.exe", ["-n", "{folder}"])));

        string written = await File.ReadAllTextAsync(Path.Combine(folder, "config.json"), TestContext.Current.CancellationToken);

        written.ShouldContain("\"executable\": \"C:\\\\Tools\\\\subl.exe\"");
        written.ShouldContain("\"arguments\": [");
        written.ShouldContain("\"{folder}\"");
    }

    [Fact]
    public async Task ReadACorruptConfigAsAbsent()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), "nope", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task RoundTripTheUpdateCheckRecord()
    {
        UpdateCheckRecord record = new(Noon, "0.10.0");

        await Store.WriteUpdateCheckAsync(record);

        (await Store.ReadUpdateCheckAsync()).ShouldBe(record);
        File.Exists(Path.Combine(folder, "update-check.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task AnswerNoUpdateCheckRecordWhenTheFileIsMissingOrCorrupt()
    {
        (await Store.ReadUpdateCheckAsync()).ShouldBeNull();

        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "update-check.json"), "{ not json", TestContext.Current.CancellationToken);

        (await Store.ReadUpdateCheckAsync()).ShouldBeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}