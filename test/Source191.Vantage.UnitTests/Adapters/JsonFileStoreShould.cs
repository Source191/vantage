using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.UnitTests.Adapters;

public sealed class JsonFileStoreShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string folder = Path.Combine(Path.GetTempPath(), "vantage-state-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider clock = new(Noon);

    private JsonFileStore Store => new(folder, clock);

    [Fact]
    public async Task StartEmptyWhenTheFolderDoesNotExist()
    {
        (await Store.ReadTabsAsync()).ShouldBeEmpty();
        (await Store.ReadRecentRepositoriesAsync()).ShouldBeEmpty();
        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
        (await Store.ReadDescriptionsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RoundTripATabRegistration()
    {
        TabRegistration pending = TabRegistration.Pending("session-1", TabPalette.Default[2], Noon);
        TabRegistration claimed = pending.ClaimedBy(4242, Noon.AddSeconds(5));

        await Store.WriteTabAsync(claimed);

        (await Store.ReadTabsAsync()).Single().ShouldBe(claimed);
    }

    [Fact]
    public async Task RemoveATabRegistration()
    {
        TabRegistration pending = TabRegistration.Pending("session-1", TabPalette.Default[2], Noon);
        await Store.WriteTabAsync(pending);

        await Store.RemoveTabAsync(pending.Id);

        (await Store.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task IgnoreACorruptTabFile()
    {
        TabRegistration pending = TabRegistration.Pending("session-1", TabPalette.Default[2], Noon);
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
    public async Task KeepDescriptionsWrittenByTwoWriters()
    {
        JsonFileStore console = Store;
        JsonFileStore here = Store;

        await Task.WhenAll(console.WriteDescriptionAsync("session-1", "Replace the login page"), here.WriteDescriptionAsync("session-2", "Spike"));

        IReadOnlyDictionary<string, string> descriptions = await Store.ReadDescriptionsAsync();
        descriptions.Count.ShouldBe(2);
        descriptions["session-1"].ShouldBe("Replace the login page");
        descriptions["session-2"].ShouldBe("Spike");
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
        await Store.WriteDescriptionAsync("session-1", "first");
        string file = Directory.EnumerateFiles(Path.Combine(folder, "descriptions")).Single();
        await using FileStream reading = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        await Store.WriteDescriptionAsync("session-1", "second");

        (await Store.ReadDescriptionsAsync())["session-1"].ShouldBe("second");
    }

    [Fact]
    public async Task TreatAHalfWrittenRecordAsAbsentUntilItIsFinished()
    {
        await Store.RecordRecentRepositoryAsync(@"C:\repos\A");
        Directory.CreateDirectory(Path.Combine(folder, "descriptions"));
        await File.WriteAllTextAsync(Path.Combine(folder, "descriptions", "half.json"), """{ "sessionId": "session-9", "descr""", TestContext.Current.CancellationToken);

        (await Store.ReadDescriptionsAsync()).ShouldBeEmpty();
        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\A"]);
    }

    [Fact]
    public async Task ImportLegacyRecentAndSessionsFiles()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "recent.json"), """["C:\\repos\\Newest", "C:\\repos\\Older"]""", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(folder, "sessions.json"), """{ "session-1": "Replace the login page" }""", TestContext.Current.CancellationToken);

        IReadOnlyList<string> recent = await Store.ReadRecentRepositoriesAsync();
        IReadOnlyDictionary<string, string> descriptions = await Store.ReadDescriptionsAsync();

        recent.ShouldBe([@"C:\repos\Newest", @"C:\repos\Older"]);
        descriptions.Count.ShouldBe(1);
        descriptions["session-1"].ShouldBe("Replace the login page");
        File.Exists(Path.Combine(folder, "recent.json")).ShouldBeFalse();
        File.Exists(Path.Combine(folder, "sessions.json")).ShouldBeFalse();
    }

    [Fact]
    public async Task RoundTripCreatedWorktreesAndForgetOne()
    {
        CreatedWorktree first = new(@"C:\repos\A-fix", "fix", @"C:\repos\A", Noon);
        CreatedWorktree second = new(@"C:\repos\B-feat", "feat", @"C:\repos\B", Noon);
        await Store.RecordCreatedWorktreeAsync(first);
        await Store.RecordCreatedWorktreeAsync(second);

        await Store.ForgetCreatedWorktreeAsync(first.Path);

        (await Store.ReadCreatedWorktreesAsync()).ShouldBe([second]);
    }

    [Fact]
    public async Task RoundTripSessionDescriptions()
    {
        await Store.WriteDescriptionAsync("session-1", "Replace the login page");
        await Store.WriteDescriptionAsync("session-2", "Spike");

        IReadOnlyDictionary<string, string> descriptions = await Store.ReadDescriptionsAsync();

        descriptions["session-1"].ShouldBe("Replace the login page");
        descriptions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task TreatACorruptDescriptionsFileAsEmpty()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "sessions.json"), "<<garbage>>", TestContext.Current.CancellationToken);

        (await Store.ReadDescriptionsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ProvideADefaultConfigurationWhenNoneIsSaved()
    {
        VantageConfig config = await Store.ReadConfigAsync();

        config.RepositoryRoots.ShouldBe([Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos")]);
        config.DefaultBase.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public async Task RoundTripTheConfiguration()
    {
        VantageConfig saved = new([@"C:\repos", @"D:\work"], WorktreeBase.Head);

        await Store.WriteConfigAsync(saved);

        (await Store.ReadConfigAsync()).ShouldBe(saved);
    }

    [Fact]
    public async Task ReadAndWriteTheWorktreeBaseAsAnEnum()
    {
        Directory.CreateDirectory(folder);
        string configFile = Path.Combine(folder, "config.json");
        await File.WriteAllTextAsync(configFile, """{ "repositoryRoots": ["C:\\repos"], "defaultBase": "Head" }""", TestContext.Current.CancellationToken);

        VantageConfig read = await Store.ReadConfigAsync();
        await Store.WriteConfigAsync(read);

        read.DefaultBase.ShouldBe(WorktreeBase.Head);
        (await File.ReadAllTextAsync(configFile, TestContext.Current.CancellationToken)).ShouldContain("\"Head\"");
    }

    [Fact]
    public async Task TreatACorruptConfigurationAsTheDefault()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "config.json"), "nope", TestContext.Current.CancellationToken);

        (await Store.ReadConfigAsync()).RepositoryRoots.Count.ShouldBe(1);
    }

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}