using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class ConsoleLoadsShould : IDisposable
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Root = @"C:\Users\me\source\repos";
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    private const string Worktree = @"C:\Users\me\source\repos\Northwind.Orders-fix-orders";
    private const string SettingsFile = @"C:\Users\me\AppData\Local\Vantage\config.json";

    // A running background session in a worktree, and an automated run (interactive, a process, no status).
    private const string Listing = """
        [{"pid":7001,"id":"81cdb0f9","cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders-fix-orders","kind":"background","startedAt":1,"sessionId":"81cdb0f9-1111","name":"fix-orders","status":"idle","state":"blocked"},
         {"pid":300,"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"interactive","startedAt":2,"sessionId":"0fe9bf3e-2222","name":"tmp-9d"}]
        """;

    private static readonly DateTimeOffset Noon = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeProcessRunner claudeRuns = new FakeProcessRunner()
        .Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, Listing, string.Empty));

    private readonly FakeProcessRunner gitRuns = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/Users/me/source/repos/Northwind.Orders-fix-orders\nHEAD b\nbranch refs/heads/fix-orders\n", string.Empty))
        .Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "0\n", string.Empty));

    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Path.Combine(Repository, ".git", "objects")).WithDirectory(Worktree);
    private readonly FakeProcessRegistry registry = new();
    private readonly InMemoryState store = new();
    private readonly FakeTimeProvider clock = new(Noon);
    private readonly CurrentSettings settings = TestSettings.Current(Root);
    private readonly PullRequestCatalogue catalogue;

    public ConsoleLoadsShould()
    {
        catalogue = new PullRequestCatalogue(new GitCli(new FakeProcessRunner().AnswerAll(new ProcessResult(2, string.Empty, string.Empty)), "git"), new FakePullRequests(), clock, new RefreshCounts(clock, new DiagnosticLog(clock, [])));
    }

    [Fact]
    public async Task RefreshTheSessionsWithTheirAutomatedRuns()
    {
        registry.InTree(300, "claude", 1).Using(300, Noon.AddSeconds(-20), MemorySize.FromMegabytes(216));

        ViewInput loaded = await Loads().LoadSessionsAsync(TestContext.Current.CancellationToken);

        RowsLoaded rows = loaded.ShouldBeOfType<RowsLoaded>();
        rows.Rows.Select(row => row.Session.Name).ShouldBe(["fix-orders"]);
        rows.AutomatedRuns.ShouldBe(new AutomatedRuns(1, MemorySize.FromMegabytes(216)));
    }

    [Fact]
    public async Task ScanWithTheLatestListingsSessions()
    {
        ConsoleLoads loads = Loads();

        await loads.LoadSessionsAsync(TestContext.Current.CancellationToken);
        ViewInput scanned = await loads.ScanWorktreesAsync(TestContext.Current.CancellationToken);

        WorktreeRow row = scanned.ShouldBeOfType<WorktreeRowsLoaded>().Rows.ShouldHaveSingleItem();
        row.Path.ShouldBe(FolderPath.From(Worktree));
        row.Session.ShouldBe(new WorktreeSession.Running(SessionId.From("81cdb0f9-1111"), "fix-orders"));
        claudeRuns.CapturedRequests.Count(request => request.Arguments.Contains("agents")).ShouldBe(1, "the scan runs no listing of its own");
    }

    [Fact]
    public async Task ReadTheConfigRowsWithTheSetupFacts()
    {
        SetupFacts nothingSetUp = new(new SetupState.At(SettingsFile), new SetupState.NotSetUp(), new SetupState.NotSetUp(), new SetupState.Disabled(), new SetupState.Never());

        ViewInput read = await Loads().ReadConfigAsync(TestContext.Current.CancellationToken);

        read.ShouldBeOfType<ConfigRowsLoaded>().Rows.ShouldBe(ConfigRows.For(settings.Value, nothingSetUp));
    }

    [Fact]
    public async Task ShowTheConfigRowsAsReadingFirst()
    {
        ConsoleLoads loads = Loads();

        IReadOnlyList<ConfigRow> before = loads.ConfigRowsWhileReading();
        await settings.SaveAsync(settings.Value.WithUpdateCheckToggled());
        IReadOnlyList<ConfigRow> after = loads.ConfigRowsWhileReading();

        before.ShouldBe(ConfigRows.For(TestSettings.For(Root), SetupFacts.Reading));
        after.ShouldBe(ConfigRows.For(TestSettings.For(Root).WithUpdateCheckToggled(), SetupFacts.Reading));
        after.ShouldNotBe(before);
    }

    private ConsoleLoads Loads()
    {
        GitCli git = new(gitRuns, "git");
        SessionRowEnricher enricher = new(git, store, registry, clock, new FakeTranscripts(), catalogue);
        WorktreeInventory inventory = new(git, folders, settings, catalogue);
        SetupFactsReader setup = new(new FakeTerminalFragment(), new FakeClaudeSettings(new NotificationChannelState.Unset()), store, SettingsFile, new DiagnosticLogSetting.Off());

        return new ConsoleLoads(new LatestSessions(new ClaudeCli(claudeRuns, Claude)), enricher, inventory, setup, settings);
    }

    public void Dispose()
    {
        catalogue.Dispose();
    }
}