using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class StartSessionHandlerShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";
    private const string Repository = @"C:\repos\Northwind.Orders";
    private const string Worktree = @"C:\repos\Northwind.Orders-fix-flaky-tests";
    private const string Started = "Starting background service…\nbackgrounded · 81cdb0f9 · Fix flaky tests (idle — send a prompt to start)\n";
    private const string Listing = """[{"pid":1,"id":"81cdb0f9","cwd":"C:\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"sess-1","name":"Fix flaky tests","status":"idle","state":"blocked"}]""";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.FileName == Claude && request.Arguments.Contains("--bg"), new ProcessResult(0, Started, string.Empty))
        .Answer(request => request.FileName == Claude && request.Arguments.Contains("agents"), new ProcessResult(0, Listing, string.Empty));
    private readonly FakeWindowsTerminal terminal = new();

    private static LaunchPlan Plan => new(
        new FolderPath(Repository), SessionName.Parse("Fix flaky tests"), Worktree: null,
        ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, OpeningPrompt: null, new RepositoryChoice("Northwind.Orders", new FolderPath(Repository), IsRepository: true));

    private StartSessionHandler Handler
    {
        get
        {
            InMemoryState store = new();
            GitCli git = new(processes);
            FakeTimeProvider clock = new(Noon);
            LaunchService service = new(new ClaudeCli(processes, Claude), git, store, store, new WorktreeIncludeCopier(git), clock);
            OpenTabHandler tabs = new(store, new FakeProcessRegistry(), terminal, clock, new TabScripts(Claude, Vantage));

            return new StartSessionHandler(service, tabs, terminal, new TabScripts(Claude, Vantage));
        }
    }

    [Fact]
    public async Task ReportTheStartWithoutATabWhenNoneWasAsked()
    {
        Outcome footer = await Handler.HandleAsync(new StartSession(Plan, OpenInTab: false), CancellationToken.None);

        footer.ShouldBe(new StartSessionOutcome.Started("Fix flaky tests", new JobId("81cdb0f9")));
        terminal.Opened.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenTheNewSessionInItsOwnTabWhenAsked()
    {
        Outcome footer = await Handler.HandleAsync(new StartSession(Plan, OpenInTab: true), CancellationToken.None);

        footer.ShouldBe(new StartSessionOutcome.StartedAndOpened("Fix flaky tests", new JobId("81cdb0f9"), new OpenTabOutcome.Opened("Fix flaky tests", "Red", ColourReused: false)));
        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Fix flaky tests");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
    }

    [Fact]
    public async Task SayHowToOpenATabWhenTheNewSessionIsNotListedYet()
    {
        processes.Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, "[]", string.Empty));

        Outcome footer = await Handler.HandleAsync(new StartSession(Plan, OpenInTab: true), CancellationToken.None);

        footer.ShouldBe(new StartSessionOutcome.NotListedYet("Fix flaky tests", new JobId("81cdb0f9")));
        terminal.Opened.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenAClaudeTabToAcceptTrustWhenTheWorkspaceIsNotTrusted()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "Workspace not trusted. Run `claude` once and accept the trust prompt, then retry."));

        Outcome footer = await Handler.HandleAsync(new StartSession(Plan, OpenInTab: true), CancellationToken.None);

        footer.ShouldBe(new StartSessionOutcome.NotTrusted(new FolderPath(Repository)));
        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Trust Northwind.Orders");
        tab.StartingDirectory.Value.ShouldBe(Repository);
    }

    [Fact]
    public async Task SayWhyAStartFailed()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "supervisor not running"));

        Outcome footer = await Handler.HandleAsync(new StartSession(Plan, OpenInTab: false), CancellationToken.None);

        footer.ShouldBe(new StartSessionOutcome.Failed("supervisor not running"));
        terminal.Opened.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartInTheWorktreeTheWizardWasFixedTo()
    {
        LaunchPlan inWorktree = Plan with { Directory = new FolderPath(Worktree), Repository = new RepositoryChoice("Northwind.Orders-fix-flaky-tests", new FolderPath(Worktree), IsRepository: true) };

        Outcome footer = await Handler.HandleAsync(new StartSession(inWorktree, OpenInTab: false), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.Arguments.Contains("--bg")).WorkingDirectory.ShouldBe(Worktree);
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("worktree"));
        footer.ShouldBe(new StartSessionOutcome.Started("Fix flaky tests", new JobId("81cdb0f9")));
    }
}