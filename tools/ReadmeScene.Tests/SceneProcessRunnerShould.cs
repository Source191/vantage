using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Adapters.Claude;
using Source191.Torni.Adapters.PullRequests;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.ReadmeScene.Tests;

public sealed class SceneProcessRunnerShould
{
    private const string Home = @"C:\TorniDemo\me";
    private static readonly SceneExecutables Executables = new(@"C:\TorniDemo\bin\readme-scene.exe", @"C:\TorniDemo\bin\gh.exe", @"C:\TorniDemo\bin\az.cmd", @"C:\Users\me\AppData\Local\Microsoft\WindowsApps\wt.exe", "readme-scene");

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeProcessRunner others = new();
    private readonly SceneProcessRunner runner;
    private readonly ClaudeCli claude;

    public SceneProcessRunnerShould()
    {
        Scene scene = SceneFile.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scene.json")));
        others.AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        runner = new SceneProcessRunner(new SceneListing(scene, new SceneLayout(Home), clock), new ScenePullRequests(scene, clock), Executables, others);
        claude = new ClaudeCli(runner, Executables.Claude);
    }

    [Fact]
    public async Task AnswerTheAgentsListing()
    {
        SessionListing listing = await claude.ListSessionsAsync(TestContext.Current.CancellationToken);

        Session waiting = listing.Sessions.Single(session => session.Name == new ListedName.Named(SessionName.From("fix-tax-rounding")));
        waiting.ShouldBeOfType<Session.Background>();
        waiting.State.ShouldBe(DisplayState.NeedsInput);
        waiting.WaitingFor.ShouldBe(new WaitingReason.Given("permission to run dotnet test"));
        waiting.WorkingDirectory.ShouldBe(FolderPath.From(@"C:\TorniDemo\me\source\worktrees\Northwind.Orders-fix-tax-rounding"));
        waiting.StartedAt.ShouldBe(clock.GetUtcNow().AddMinutes(-95));
        listing.Sessions.Count.ShouldBe(8);
        listing.Sessions.Single(session => session.Name == new ListedName.Named(SessionName.From("explore-reporting"))).ShouldBeOfType<Session.Interactive>()
            .WorkingDirectory.ShouldBe(FolderPath.From(@"C:\TorniDemo\me\source\repos\Northwind.Portal"));
        listing.Sessions.Select(session => session.State.Kind).Distinct().Count().ShouldBe(6);
        listing.AutomatedRuns.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task NameEverySessionsTranscript()
    {
        SceneListing sceneListing = new(SceneFile.Read(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "scene.json"), TestContext.Current.CancellationToken)), new SceneLayout(Home), clock);
        ClaudeCli sceneClaude = new(new SceneProcessRunner(sceneListing, new ScenePullRequests(SceneFile.Read("""{ "claudeVersion": "2.1.316" }"""), clock), Executables, others), Executables.Claude);

        SessionListing listing = await sceneClaude.ListSessionsAsync(TestContext.Current.CancellationToken);

        sceneListing.SessionIds().ShouldBe(listing.Sessions.Select(session => session.SessionId.Value), ignoreOrder: true);
    }

    [Fact]
    public async Task AnswerTheClaudeVersion()
    {
        ClaudeVersionReport version = await claude.VersionAsync(TestContext.Current.CancellationToken);

        version.ShouldBe(new ClaudeVersionReport.Printed("2.1.316"));
    }

    [Fact]
    public async Task ListAStartedSessionNextTime()
    {
        FolderPath folder = FolderPath.From(@"C:\TorniDemo\me\source\worktrees\Northwind.Inventory-add-stock-alerts");

        StartResult started = await claude.StartBackgroundAsync(folder, SessionName.From("add-stock-alerts"), new OpeningPrompt.Text("Email the buyer when stock runs low"), TestContext.Current.CancellationToken);
        SessionListing first = await claude.ListSessionsAsync(TestContext.Current.CancellationToken);
        SessionListing second = await claude.ListSessionsAsync(TestContext.Current.CancellationToken);

        JobId jobId = started.ShouldBeOfType<StartResult.Started>().JobId;
        Session.Background starting = first.Sessions.OfType<Session.Background>().Single(session => session.JobId == jobId);
        starting.State.ShouldBeOfType<DisplayState.Starting>();
        starting.WorkingDirectory.ShouldBe(folder);
        second.Sessions.OfType<Session.Background>().Single(session => session.JobId == jobId).State.ShouldBe(DisplayState.Working);
    }

    [Fact]
    public async Task AnswerGitHubPullRequests()
    {
        GitHubPullRequests gitHub = new(runner, new ProgramLocation.Found(Executables.GitHub));

        PullRequestListing listing = await gitHub.ListAsync(
            new RemoteRepository.GitHub(SceneFile.DemoOwner, "Northwind.Orders"),
            FolderPath.From(@"C:\TorniDemo\me\source\worktrees\Northwind.Orders-fix-tax-rounding"),
            BranchName.From("fix-tax-rounding"),
            TestContext.Current.CancellationToken);

        PullRequest pullRequest = listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldHaveSingleItem().PullRequest;
        pullRequest.Number.ShouldBe(142);
        pullRequest.State.ShouldBe(PullRequestState.Open);
        pullRequest.Host.ShouldBe(PullRequestHost.GitHub);
    }

    [Fact]
    public async Task AnswerAzureDevOpsPullRequests()
    {
        AzureDevOpsPullRequests azureDevOps = new(runner, new ProgramLocation.Found(Executables.AzureDevOps));

        PullRequestListing listing = await azureDevOps.ListAsync(
            new RemoteRepository.AzureDevOps(SceneFile.DemoOwner, "Northwind", "Northwind.Billing"),
            FolderPath.From(@"C:\TorniDemo\me\source\worktrees\Northwind.Billing-invoice-pdf-export"),
            BranchName.From("invoice-pdf-export"),
            TestContext.Current.CancellationToken);

        PullRequest pullRequest = listing.ShouldBeOfType<PullRequestListing.Listed>().PullRequests.ShouldHaveSingleItem().PullRequest;
        pullRequest.Number.ShouldBe(318);
        pullRequest.State.ShouldBe(PullRequestState.Draft);
        pullRequest.Host.ShouldBe(PullRequestHost.AzureDevOps);
    }

    [Fact]
    public async Task RefuseEveryOtherClaudeCommand()
    {
        await Should.ThrowAsync<ClaudeCliException>(() => claude.StopAsync(JobId.From("1a2b3c4d"), TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ClaudeCliException>(() => claude.RemoveAsync(JobId.From("1a2b3c4d"), TestContext.Current.CancellationToken));
        (await claude.AttachAsync(JobId.From("1a2b3c4d"), TestContext.Current.CancellationToken)).ShouldNotBe(0);

        others.CapturedRequests.ShouldBeEmpty();
        others.InteractiveRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendEveryTabToTheScenesOwnWindow()
    {
        ProcessRequest newTab = new(Executables.Terminal, ["-w", "0", "new-tab", "--title", "stock-alerts"], WorkingFolder.Inherit);

        await runner.RunAsync(newTab, TestContext.Current.CancellationToken);

        others.CapturedRequests.ShouldHaveSingleItem().Arguments.ShouldBe(["-w", Executables.WindowName, "new-tab", "--title", "stock-alerts"]);
    }

    [Fact]
    public async Task HandEveryOtherProgramToTheRealRunner()
    {
        ProcessRequest git = new(@"C:\Program Files\Git\cmd\git.exe", ["status"], WorkingFolder.Inherit);

        await runner.RunAsync(git, TestContext.Current.CancellationToken);

        others.CapturedRequests.ShouldHaveSingleItem().ShouldBe(git);
    }
}