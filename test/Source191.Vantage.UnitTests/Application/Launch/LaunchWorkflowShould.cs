using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Application.Launch;

public sealed class LaunchWorkflowShould : IDisposable
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", @"C:\repos\Northwind.Orders", IsRepository: true);
    private static readonly Session Listed = new("sess-1", "81cdb0f9", "Fix flaky tests", Repository.Path, SessionKind.Background, Noon, DisplayState.Working);

    private readonly string stateFolder = Path.Combine(Path.GetTempPath(), "vantage-workflow-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeWindowsTerminal terminal = new();

    private static LaunchPlan Plan => new(
        Repository.Path, SessionName.Parse("Fix flaky tests"), Description: null, Worktree: null,
        ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, OpeningPrompt: null, Repository);

    private static LaunchOutcome Started(Session? session) => new(LaunchOutcomeKind.Started, "81cdb0f9", "backgrounded", session);

    private LaunchWorkflow Workflow
    {
        get
        {
            StateStore store = new(stateFolder, TimeProvider.System);
            GitCli git = new(processes);
            FakeTimeProvider clock = new(Noon);
            LaunchService service = new(new ClaudeCli(processes, Claude), git, store, new WorktreeIncludeCopier(git), clock);
            TabOpener tabs = new(store, new FakeProcessRegistry(), terminal, clock, Claude, Vantage);

            return new LaunchWorkflow(new RepositoryCatalogue([]), store, service, tabs, terminal, Claude);
        }
    }

    [Fact]
    public async Task ReportTheStartWithoutATabWhenNoneWasAsked()
    {
        string footer = await Workflow.StartedAsync(Plan, Started(Listed), openInTab: false, CancellationToken.None);

        footer.ShouldBe("Started Fix flaky tests (81cdb0f9).");
        terminal.Opened.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenTheNewSessionInItsOwnTabWhenAsked()
    {
        string footer = await Workflow.StartedAsync(Plan, Started(Listed), openInTab: true, CancellationToken.None);

        footer.ShouldBe("Started Fix flaky tests (81cdb0f9). Opened Fix flaky tests in a red tab.");
        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Fix flaky tests");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
    }

    [Fact]
    public async Task SayHowToOpenATabWhenTheNewSessionIsNotListedYet()
    {
        string footer = await Workflow.StartedAsync(Plan, Started(session: null), openInTab: true, CancellationToken.None);

        footer.ShouldBe("Started Fix flaky tests (81cdb0f9). It is not listed yet: press o on it to open a tab.");
        terminal.Opened.ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(stateFolder))
        {
            Directory.Delete(stateFolder, recursive: true);
        }
    }
}