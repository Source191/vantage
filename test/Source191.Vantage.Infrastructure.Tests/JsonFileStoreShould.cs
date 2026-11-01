using Microsoft.Extensions.Time.Testing;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

[SupportedOSPlatform("windows")]
public sealed class JsonFileStoreShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid SessionTabId = Guid.Parse("0b8d2a1c-9e3f-4b5a-8c7d-6e5f4a3b2c1d");
    private static readonly Guid OwnTabId = Guid.Parse("1c9e3f4b-5a8c-7d6e-5f4a-3b2c1d0b8d2a");

    private static readonly TabRegistration SessionTab = new(SessionTabId, new TabOwner.ForSession(SessionId.From("1a4d2d86-8e87-44fe-a9f7-61d47d481ae3")), TabPalette.Default.Single(colour => colour.Name == "Yellow"), Noon, new TabClaim.Claimed(4242, Noon.AddSeconds(5)));
    private static readonly TabRegistration OwnTab = new(OwnTabId, TabOwner.Vantage, TabPalette.Default.Single(colour => colour.Name == "Green"), Noon, new TabClaim.Claimed(5151, Noon.AddSeconds(1)));

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

    /// <summary>A session's tab file as the store writes it before the tab's shell claims it: no process yet.</summary>
    private const string PendingTabJson = """
        {
          "id": "0b8d2a1c-9e3f-4b5a-8c7d-6e5f4a3b2c1d",
          "sessionId": "1a4d2d86-8e87-44fe-a9f7-61d47d481ae3",
          "colour": {
            "name": "Yellow",
            "hex": "#FFB900"
          },
          "createdAt": "2026-10-23T12:00:00+00:00",
          "processId": null,
          "processStartedAt": null
        }
        """;

    private static readonly TabRegistration PendingTab = SessionTab with { Claim = new TabClaim.Pending() };

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

    /// <summary>One file under created\ exactly as the store writes it: the object of one element of CreatedJson.</summary>
    private const string CreatedRecordJson = """
        {
          "path": "C:\\Users\\me\\source\\repos\\Northwind.Orders-fix-123",
          "branch": "fix-123",
          "repository": "C:\\Users\\me\\source\\repos\\Northwind.Orders",
          "createdAt": "2026-10-23T12:00:00+00:00"
        }
        """;

    /// <summary>config.json exactly as the store wrote it before a missing file read as VantageConfig.Missing: every member named.</summary>
    private const string ConfigJson = """
        {
          "repositoryRoots": [
            "C:\\Users\\me\\source\\repos",
            "D:\\work"
          ],
          "defaultBase": "RemoteDefault",
          "worktreeRoot": "C:\\Users\\me\\source\\worktrees",
          "editor": {
            "preset": "VsCode",
            "executable": null,
            "arguments": null
          },
          "checkForUpdates": true,
          "defaultPlace": "NewWorktree"
        }
        """;

    private static readonly VantageConfig SavedConfig = new([@"C:\Users\me\source\repos", @"D:\work"], WorktreeBase.RemoteDefault, @"C:\Users\me\source\worktrees", new EditorSetting("VsCode", Executable: null, Arguments: null), CheckForUpdates: true, DefaultPlace: LaunchMode.NewWorktree);

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
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default.Single(colour => colour.Name == "Yellow"), Noon);
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
    public async Task ReadAPendingTabFileWrittenByTheCurrentVersion()
    {
        Directory.CreateDirectory(Path.Combine(folder, "tabs"));
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs", "0b8d2a1c9e3f4b5a8c7d6e5f4a3b2c1d.json"), PendingTabJson, TestContext.Current.CancellationToken);

        TabRegistration read = (await Store.ReadTabsAsync()).Single();

        read.ShouldBe(PendingTab);
        read.Claim.ShouldBe(new TabClaim.Pending());
    }

    [Fact]
    public async Task ReadATabFileHoldingAProcessWithoutItsStartTimeAsPending()
    {
        Directory.CreateDirectory(Path.Combine(folder, "tabs"));
        await File.WriteAllTextAsync(Path.Combine(folder, "tabs", "0b8d2a1c9e3f4b5a8c7d6e5f4a3b2c1d.json"), PendingTabJson.Replace("\"processId\": null", "\"processId\": 4242", StringComparison.Ordinal), TestContext.Current.CancellationToken);

        (await Store.ReadTabsAsync()).Single().Claim.ShouldBe(new TabClaim.Pending());
    }

    [Fact]
    public async Task WriteTheTabFileItAlwaysHas()
    {
        await Store.WriteTabAsync(PendingTab);

        (await File.ReadAllTextAsync(Path.Combine(folder, "tabs", $"{SessionTabId:N}.json"), TestContext.Current.CancellationToken)).ReplaceLineEndings("\n").ShouldBe(PendingTabJson.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task RemoveATabRegistration()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default.Single(colour => colour.Name == "Yellow"), Noon);
        await Store.WriteTabAsync(pending);

        await Store.RemoveTabAsync(pending.Id);

        (await Store.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task IgnoreACorruptTabFile()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default.Single(colour => colour.Name == "Yellow"), Noon);
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
    public async Task LeaveNoTemporaryFileBehind()
    {
        await Store.WriteConfigAsync(SavedConfig);
        await Store.WriteConfigAsync(SavedConfig with { CheckForUpdates = false });
        await Store.WriteTabAsync(SessionTab);
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        await Store.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon, new NewestRelease.None()));
        await Store.WriteLastRunAsync(new ReleaseVersion(0, 21, 0, ""));

        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ShouldAllBe(file => file.EndsWith(".json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeepTheOldFileAndNoTemporaryWhenTheReplaceIsRefused()
    {
        await Store.WriteConfigAsync(SavedConfig);
        await using FileStream holding = new(Path.Combine(folder, "config.json"), FileMode.Open, FileAccess.Read, FileShare.Read);

        await Should.ThrowAsync<IOException>(() => Store.WriteConfigAsync(SavedConfig with { CheckForUpdates = false }));

        await holding.DisposeAsync();
        (await Store.ReadConfigAsync()).ShouldBe(SavedConfig);
        Directory.GetFiles(folder).Select(Path.GetFileName).ShouldBe(["config.json"]);
    }

    [Fact]
    public async Task ReadAFileItIsNotAllowedToOpenAsAbsent()
    {
        await Store.WriteConfigAsync(SavedConfig);
        FileInfo file = new(Path.Combine(folder, "config.json"));
        SecurityIdentifier user = WindowsIdentity.GetCurrent().User!;
        FileSystemAccessRule deny = new(user, FileSystemRights.ReadData, AccessControlType.Deny);
        FileSecurity security = file.GetAccessControl();
        security.AddAccessRule(deny);
        file.SetAccessControl(security);

        try
        {
            (await Store.ReadConfigAsync()).ShouldBe(VantageConfig.Missing);
        }
        finally
        {
            security.RemoveAccessRule(deny);
            file.SetAccessControl(security);
        }
    }

    [Fact]
    public async Task ReadAConfigHoldingANumberForTheBaseAsMissing()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), """{ "repositoryRoots": ["D:\\work"], "defaultBase": 7 }""", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).ShouldBe(VantageConfig.Missing);
    }

    [Fact]
    public async Task ReadAConfigHoldingANumberForThePlaceAsMissing()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), """{ "repositoryRoots": ["D:\\work"], "defaultPlace": 7 }""", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).ShouldBe(VantageConfig.Missing);
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
    public async Task KeepCreatedWorktreesWrittenByTwoWriters()
    {
        JsonFileStore console = Store;
        JsonFileStore here = new(folder, new FakeTimeProvider(Noon.AddSeconds(1)));
        CreatedWorktree first = new(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon);
        CreatedWorktree second = new(FolderPath.From(@"C:\repos\B-feat"), BranchName.From("feat"), FolderPath.From(@"C:\repos\B"), Noon);

        await Task.WhenAll(console.RecordCreatedWorktreeAsync(first), here.RecordCreatedWorktreeAsync(second));

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([first, second], ignoreOrder: true);
        Directory.EnumerateFiles(Path.Combine(folder, "created")).Count().ShouldBe(2);
    }

    [Fact]
    public async Task ReplaceACreatedWorktreeRecordedUnderAnotherSpelling()
    {
        CreatedWorktree first = new(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon);
        CreatedWorktree later = new(FolderPath.From(@"c:\repos\a-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon.AddHours(1));

        await Store.RecordCreatedWorktreeAsync(first);
        await Store.RecordCreatedWorktreeAsync(later);

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([later]);
    }

    [Fact]
    public async Task ForgetACreatedWorktreeWhileAnotherProcessRecordsOne()
    {
        CreatedWorktree forgotten = new(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon);
        CreatedWorktree recorded = new(FolderPath.From(@"C:\repos\B-feat"), BranchName.From("feat"), FolderPath.From(@"C:\repos\B"), Noon);
        await Store.RecordCreatedWorktreeAsync(forgotten);
        JsonFileStore here = new(folder, clock);

        await Task.WhenAll(Store.ForgetCreatedWorktreeAsync(forgotten.Path), here.RecordCreatedWorktreeAsync(recorded));

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([recorded]);
    }

    [Fact]
    public async Task ImportTheLegacyCreatedFile()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "created.json"), CreatedJson, TestContext.Current.CancellationToken);

        CreatedWorktree created = (await Store.ReadCreatedWorktreesAsync()).Single();

        created.ShouldBe(new CreatedWorktree(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123"), BranchName.From("fix-123"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon));
        File.Exists(Path.Combine(folder, "created.json")).ShouldBeFalse();
        Directory.EnumerateFiles(Path.Combine(folder, "created")).Count().ShouldBe(1);
    }

    [Fact]
    public async Task ForgetAnImportedCreatedWorktreeForGood()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "created.json"), CreatedJson, TestContext.Current.CancellationToken);

        await Store.ForgetCreatedWorktreeAsync(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123"));

        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
        File.Exists(Path.Combine(folder, "created.json")).ShouldBeFalse();
    }

    [Fact]
    public async Task WriteACreatedWorktreeFileTheCurrentVersionCanRead()
    {
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123"), BranchName.From("fix-123"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon));

        string written = await File.ReadAllTextAsync(Directory.EnumerateFiles(Path.Combine(folder, "created")).Single(), TestContext.Current.CancellationToken);

        written.ReplaceLineEndings("\n").ShouldBe(CreatedRecordJson.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task TreatACreatedFileHoldingARelativePathAsCorrupt()
    {
        CreatedWorktree good = new(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Noon);
        await Store.RecordCreatedWorktreeAsync(good);
        await File.WriteAllTextAsync(Path.Combine(folder, "created", "relative.json"), CreatedRecordJson.Replace(@"C:\\Users\\me\\source\\repos\\Northwind.Orders-fix-123", @"Northwind.Orders-fix-123", StringComparison.Ordinal), TestContext.Current.CancellationToken);

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([good]);
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

        VantageConfig read = (await Store.ReadConfigAsync());

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

        (await Store.ReadConfigAsync()).DefaultBase.ShouldBeNull();
    }

    [Fact]
    public async Task ReadAndWriteTheWorktreeBaseAsAnEnum()
    {
        Directory.CreateDirectory(folder);
        string configFile = Path.Combine(folder, "config.json");
        await File.WriteAllTextAsync(configFile, """{ "repositoryRoots": ["C:\\repos"], "defaultBase": "Head" }""", TestContext.Current.CancellationToken);

        VantageConfig read = (await Store.ReadConfigAsync());
        await Store.WriteConfigAsync(read);

        read.DefaultBase.ShouldBe(WorktreeBase.Head);
        (await File.ReadAllTextAsync(configFile, TestContext.Current.CancellationToken)).ShouldContain("\"Head\"");
    }

    [Fact]
    public async Task ReadAndWriteTheDefaultPlaceByName()
    {
        Directory.CreateDirectory(folder);
        string configFile = Path.Combine(folder, "config.json");
        await File.WriteAllTextAsync(configFile, """{ "repositoryRoots": ["C:\\repos"], "defaultPlace": "CurrentBranch" }""", TestContext.Current.CancellationToken);

        VantageConfig read = (await Store.ReadConfigAsync());
        await Store.WriteConfigAsync(read);

        read.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
        (await File.ReadAllTextAsync(configFile, TestContext.Current.CancellationToken)).ShouldContain("\"defaultPlace\": \"CurrentBranch\"");
    }

    [Fact]
    public async Task ReadAConfigWithoutAPlaceAsHavingNone()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), """{ "repositoryRoots": ["D:\\work"] }""", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).DefaultPlace.ShouldBeNull();
    }

    [Fact]
    public async Task NameTheSettingsFileItWrites()
    {
        await Store.WriteConfigAsync(new VantageConfig([@"C:\repos"], WorktreeBase.RemoteDefault, @"C:\worktrees", new EditorSetting("VsCode", Executable: null, Arguments: null)));

        Store.ConfigFile.ShouldBe(Path.Combine(folder, "config.json"));
        File.Exists(Store.ConfigFile).ShouldBeTrue();
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
    public async Task ReadAMissingOrCorruptConfigAsMissing()
    {
        (await Store.ReadConfigAsync()).ShouldBe(VantageConfig.Missing);

        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), "nope", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).ShouldBe(VantageConfig.Missing);
    }

    [Fact]
    public async Task ReadAConfigFileWrittenByThePreviousVersion()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), ConfigJson, TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).ShouldBe(SavedConfig);
    }

    [Fact]
    public async Task WriteTheConfigFileAsBefore()
    {
        await Store.WriteConfigAsync(SavedConfig);

        string written = await File.ReadAllTextAsync(Path.Combine(folder, "config.json"), TestContext.Current.CancellationToken);

        written.ReplaceLineEndings("\n").ShouldBe(ConfigJson.ReplaceLineEndings("\n"));
    }

    /// <summary>The newest member's JSON in update-check.json and the release it stands for.</summary>
    public static TheoryData<string, NewestRelease> RecordedNewest => new()
    {
        { "\"0.10.0\"", new NewestRelease.Found(new ReleaseVersion(0, 10, 0, "")) },
        { "null", new NewestRelease.None() },
        { "\"rubbish\"", new NewestRelease.Unreadable("rubbish") },
    };

    [Theory]
    [MemberData(nameof(RecordedNewest))]
    public async Task ReadAnUpdateCheckFileWrittenByThePreviousVersion(string newestJson, NewestRelease newest)
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "update-check.json"), UpdateCheckJson(newestJson), TestContext.Current.CancellationToken);

        (await Store.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, newest));
    }

    [Theory]
    [MemberData(nameof(RecordedNewest))]
    public async Task WriteTheUpdateCheckFileAsBefore(string newestJson, NewestRelease newest)
    {
        await Store.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon, newest));

        string written = await File.ReadAllTextAsync(Path.Combine(folder, "update-check.json"), TestContext.Current.CancellationToken);

        written.ReplaceLineEndings("\n").ShouldBe(UpdateCheckJson(newestJson).ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task RoundTripTheUpdateCheckRecord()
    {
        UpdateCheckRecord.Checked record = new(Noon, new NewestRelease.Found(new ReleaseVersion(0, 10, 0, "")));

        await Store.WriteUpdateCheckAsync(record);

        (await Store.ReadUpdateCheckAsync()).ShouldBe(record);
        File.Exists(Path.Combine(folder, "update-check.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task KeepTheNewestReleasesNotes()
    {
        ReleaseNotes.TryParse("## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)", out ReleaseNotes? notes).ShouldBeTrue();
        UpdateCheckRecord.Checked record = new(Noon, new NewestRelease.Found(new ReleaseVersion(0, 21, 0, "")), notes);

        await Store.WriteUpdateCheckAsync(record);

        UpdateCheckRecord.Checked read = (await Store.ReadUpdateCheckAsync()).ShouldBeOfType<UpdateCheckRecord.Checked>();
        read.ShouldBe(record);
        read.NewestNotes.Sections.Single().Entries.Single().Text.ShouldBe("A key that opens the Northwind order (#171)");
    }

    [Fact]
    public async Task ReadAnUpdateCheckFileWithoutNotesAsHavingNone()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "update-check.json"), UpdateCheckJson("\"0.21.0\""), TestContext.Current.CancellationToken);

        UpdateCheckRecord.Checked read = (await Store.ReadUpdateCheckAsync()).ShouldBeOfType<UpdateCheckRecord.Checked>();

        read.NewestNotes.ShouldBe(ReleaseNotes.None);
    }

    [Fact]
    public async Task ReadNotesThatAreNotAChangelogAsNone()
    {
        Directory.CreateDirectory(folder);
        string json = """
            {
              "checkedAt": "2026-10-23T12:00:00+00:00",
              "newest": "0.21.0",
              "notes": "Release notes: https://github.com/Source191/vantage/releases/tag/v0.21.0"
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(folder, "update-check.json"), json, TestContext.Current.CancellationToken);

        UpdateCheckRecord.Checked read = (await Store.ReadUpdateCheckAsync()).ShouldBeOfType<UpdateCheckRecord.Checked>();

        read.NewestNotes.ShouldBe(ReleaseNotes.None);
    }

    [Fact]
    public async Task RecordTheLastVersionRun()
    {
        (await Store.ReadLastRunAsync()).ShouldBe(new LastRun.Never());

        await Store.WriteLastRunAsync(new ReleaseVersion(0, 21, 0, ""));

        (await Store.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(new ReleaseVersion(0, 21, 0, "")));
        string written = await File.ReadAllTextAsync(Path.Combine(folder, "last-run.json"), TestContext.Current.CancellationToken);
        written.ReplaceLineEndings("\n").ShouldBe("{\n  \"version\": \"0.21.0\"\n}");
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{ \"version\": \"rubbish\" }")]
    [InlineData("{ \"version\": null }")]
    [InlineData("{}")]
    public async Task ReadAnUnreadableLastRunAsNever(string json)
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "last-run.json"), json, TestContext.Current.CancellationToken);

        (await Store.ReadLastRunAsync()).ShouldBe(new LastRun.Never());
    }
    [Fact]
    public async Task ReadAMissingOrCorruptUpdateCheckAsNever()
    {
        (await Store.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Never());

        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "update-check.json"), "{ not json", TestContext.Current.CancellationToken);

        (await Store.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Never());
    }

    /// <summary>update-check.json exactly as the store wrote it before the record had named cases, with the newest member's JSON.</summary>
    private static string UpdateCheckJson(string newestJson) => $$"""
        {
          "checkedAt": "2026-10-23T12:00:00+00:00",
          "newest": {{newestJson}}
        }
        """;

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}