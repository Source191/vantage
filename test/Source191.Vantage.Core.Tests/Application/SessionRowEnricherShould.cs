using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SessionRowEnricherShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(128, string.Empty, "fatal: not a git repository"));
    private readonly FakeProcessRegistry registry = new();
    private readonly FakeTimeProvider clock = new(Noon);

    private readonly FakeTranscripts transcripts = new();
    private readonly InMemoryState store = new();

    private readonly FakeProcessRunner remotes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, "https://github.com/northwind/Northwind.Orders.git\n", string.Empty));
    private readonly FakePullRequests hosts = new();
    private readonly PullRequestCatalogue catalogue;

    public SessionRowEnricherShould()
    {
        catalogue = new PullRequestCatalogue(new GitCli(remotes, "git"), hosts, clock);
    }

    private SessionRowEnricher Enricher => new(new GitCli(processes, "git"), store, registry, clock, transcripts, catalogue);

    [Fact]
    public async Task CarryEachRowsPullRequest()
    {
        PullRequest open = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, Noon);
        hosts.Answer(new GitHubRepository("northwind", "Northwind.Orders"), new PullRequestListing.Listed([new BranchPullRequest(BranchName.From("fix"), open)]));
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session session = Session(@"C:\repos\Northwind.Orders-fix");
        SessionRowEnricher enricher = Enricher;

        SessionRow first = (await enricher.EnrichAsync([session], CancellationToken.None)).Single();
        await catalogue.RefreshStaleAsync([(FolderPath.From(@"C:\repos\Northwind.Orders"), BranchName.From("fix"))]);
        SessionRow second = (await enricher.EnrichAsync([session], CancellationToken.None)).Single();

        first.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
        second.PullRequest.ShouldBe(new PullRequestLookup.Found(open));
        hosts.Calls.Count.ShouldBe(1);
        remotes.CapturedRequests.Single().WorkingDirectory.ShouldBe(@"C:\repos\Northwind.Orders");
    }

    [Fact]
    public async Task LookUpEachRowsBranch()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        AnswerGit(@"C:\repos\Northwind.Orders-spike", root: "C:/repos/Northwind.Orders-spike", commonDir: "C:/repos/Northwind.Orders/.git", branch: string.Empty);

        await Enricher.EnrichAsync([Session(@"C:\repos\Northwind.Orders-fix"), Session(@"C:\repos\Northwind.Orders-spike")], CancellationToken.None);
        while (hosts.Requests.Count < 1)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);
        hosts.Requests.Select(request => request.Branch.Value).ShouldBe(["fix"], "a detached head has no branch to look up");
    }

    [Fact]
    public async Task NotWaitForTheLookup()
    {
        hosts.Hold();
        AnswerGit(@"C:\repos\Northwind.Orders", root: "C:/repos/Northwind.Orders", commonDir: "C:/repos/Northwind.Orders/.git", branch: "main");

        IReadOnlyList<SessionRow> rows = await Enricher.EnrichAsync([Session(@"C:\repos\Northwind.Orders")], CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        rows.Single().PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
        hosts.Release();
    }

    [Fact]
    public async Task CarryTheMemoryOfARunningSessionsProcessTree()
    {
        registry.InTree(100, "claude", 1).Using(100, Noon.AddMinutes(-10), MemorySize.FromMegabytes(296))
            .InTree(200, "cmd", 100).Using(200, Noon.AddMinutes(-9), MemorySize.FromMegabytes(607));
        BackgroundSession running = Session(@"C:\repos\Northwind.Orders") with { ProcessId = 100 };

        IReadOnlyList<SessionRow> rows = await Enricher.EnrichAsync([running], CancellationToken.None);

        rows.Single().Memory.ShouldBe(new SessionMemory.Measured(MemorySize.FromMegabytes(903)));
    }

    [Fact]
    public void CountTheAutomatedRunsAndAddUpTheMemoryOfTheirTrees()
    {
        registry.InTree(300, "claude", 1).Using(300, Noon.AddSeconds(-20), MemorySize.FromMegabytes(200))
            .InTree(301, "node", 300).Using(301, Noon.AddSeconds(-19), MemorySize.FromMegabytes(15))
            .InTree(400, "claude", 1).Using(400, Noon.AddSeconds(-5), MemorySize.FromMegabytes(216));

        AutomatedRuns runs = Enricher.Measure([Run(300), Run(400)]);

        runs.ShouldBe(new AutomatedRuns(2, MemorySize.FromMegabytes(431)));
    }

    [Fact]
    public void CountARunWhoseProcessHasGoneWithoutItsMemory()
    {
        registry.InTree(400, "claude", 1).Using(400, Noon.AddSeconds(-5), MemorySize.FromMegabytes(216));

        AutomatedRuns runs = Enricher.Measure([Run(400), Run(401)]);

        runs.ShouldBe(new AutomatedRuns(2, MemorySize.FromMegabytes(216)));
    }

    [Fact]
    public void MeasureNoRunsAsNone()
    {
        Enricher.Measure([]).ShouldBe(AutomatedRuns.None);
    }

    [Fact]
    public async Task CarryNothingToMeasureForASessionWithoutAProcessOrWhoseProcessHasGone()
    {
        registry.InTree(100, "claude", 1).Using(100, Noon.AddMinutes(-10), MemorySize.FromMegabytes(296));
        BackgroundSession stopped = Session(@"C:\repos\Northwind.Orders");
        BackgroundSession gone = Session(@"C:\repos\Northwind.Orders") with { ProcessId = 101 };
        InteractiveSession terminal = new(SessionId.From("terminal"), "plain", FolderPath.From(@"C:\repos\Northwind.Orders"), Noon, DisplayState.Idle, ProcessId: 100);

        IReadOnlyList<SessionRow> rows = await Enricher.EnrichAsync([stopped, gone, terminal], CancellationToken.None);

        rows[0].Memory.ShouldBe(new SessionMemory.NothingToMeasure());
        rows[1].Memory.ShouldBe(new SessionMemory.NothingToMeasure());
        rows[2].Memory.ShouldBe(new SessionMemory.Measured(MemorySize.FromMegabytes(296)));
    }

    [Fact]
    public async Task TakeOneSnapshotOfTheProcessTablePerRefresh()
    {
        registry.InTree(100, "claude", 1).Using(100, Noon, MemorySize.FromMegabytes(1)).InTree(101, "claude", 1).Using(101, Noon, MemorySize.FromMegabytes(2));
        BackgroundSession first = Session(@"C:\repos\Northwind.Orders") with { ProcessId = 100 };
        BackgroundSession second = Session(@"C:\repos\Northwind.Orders") with { ProcessId = 101 };

        await Enricher.EnrichAsync([first, second], CancellationToken.None);

        registry.Snapshots.ShouldBe(1);
    }

    [Fact]
    public async Task SayWhetherASessionHasATranscriptToResumeFrom()
    {
        Session withMessages = Session(@"C:\repos\Northwind.Orders");
        Session untouched = Session(@"C:\repos\Northwind.Orders") with { SessionId = SessionId.From("never-sent-anything") };
        transcripts.WithMessages(withMessages.SessionId.Value);

        IReadOnlyList<SessionRow> rows = await Enricher.EnrichAsync([withMessages, untouched], CancellationToken.None);

        rows[0].HasTranscript.ShouldBeTrue();
        rows[1].HasTranscript.ShouldBeFalse();
    }

    [Fact]
    public async Task NameTheRepositoryAndBranchOfASessionInAMainCheckout()
    {
        AnswerGit(@"C:\repos\Northwind.Orders", root: "C:/repos/Northwind.Orders", commonDir: "C:/repos/Northwind.Orders/.git", branch: "feature/x");
        Session session = Session(@"C:\repos\Northwind.Orders");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("Northwind.Orders");
        row.Head.ShouldBe(new Head.OnBranch(BranchName.From("feature/x")));
    }

    [Fact]
    public async Task NameTheParentRepositoryOfASessionInALinkedWorktree()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session session = Session(@"C:\repos\Northwind.Orders-fix");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("Northwind.Orders");
        row.Head.ShouldBe(new Head.OnBranch(BranchName.From("fix")));
    }

    [Fact]
    public async Task KnowNoBranchOfASessionWhoseRepositoryNamesNone()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-spike", root: "C:/repos/Northwind.Orders-spike", commonDir: "C:/repos/Northwind.Orders/.git", branch: string.Empty);
        Session session = Session(@"C:\repos\Northwind.Orders-spike");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("Northwind.Orders");
        row.Head.ShouldBe(new Head.Unknown());
        row.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
    }

    [Fact]
    public async Task FallBackToTheFolderNameOutsideAnyRepository()
    {
        Session session = Session(@"C:\Users\me\source\repos");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("repos");
        row.Head.ShouldBe(new Head.Unknown());
        row.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
    }

    [Fact]
    public async Task MarkASessionWithALiveVantageTab()
    {
        Session session = Session(@"C:\repos\Northwind.Orders");
        TabRegistration live = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), TabPalette.Default[0], Noon.AddMinutes(-5)).ClaimedBy(7001, Noon.AddMinutes(-5));
        TabRegistration stale = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), TabPalette.Default[1], Noon.AddHours(-1));
        await store.WriteTabAsync(live);
        await store.WriteTabAsync(stale);
        registry.Running(7001, Noon.AddMinutes(-5));

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Tab.ShouldBe(new LiveTab.Open(TabPalette.Default[0]));
        (await store.ReadTabsAsync()).Count.ShouldBe(1, "the stale registration is removed on read");
    }

    [Fact]
    public async Task NotTakeVantagesOwnTabForASessionsTab()
    {
        Session session = Session(@"C:\repos\Northwind.Orders");
        await store.WriteTabAsync(TabRegistration.Pending(TabOwner.Vantage, TabPalette.Default[4], Noon.AddMinutes(-5)).ClaimedBy(7001, Noon.AddMinutes(-5)));
        registry.Running(7001, Noon.AddMinutes(-5));

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Tab.ShouldBe(new LiveTab.None());
        (await store.ReadTabsAsync()).ShouldHaveSingleItem("Vantage's own live tab is kept");
    }

    [Fact]
    public async Task AskGitOnceForAWorkingDirectoryWithinTheCacheWindow()
    {
        AnswerGit(@"C:\repos\Northwind.Orders", root: "C:/repos/Northwind.Orders", commonDir: "C:/repos/Northwind.Orders/.git", branch: "main");
        Session first = Session(@"C:\repos\Northwind.Orders");
        Session second = Session(@"C:\repos\Northwind.Orders");
        SessionRowEnricher enricher = Enricher;

        await enricher.EnrichAsync([first, second], CancellationToken.None);
        int afterFirst = processes.CapturedRequests.Count;
        clock.Advance(TimeSpan.FromSeconds(10));
        await enricher.EnrichAsync([first], CancellationToken.None);
        int afterSecond = processes.CapturedRequests.Count;
        clock.Advance(TimeSpan.FromSeconds(31));
        await enricher.EnrichAsync([first], CancellationToken.None);

        afterSecond.ShouldBe(afterFirst);
        processes.CapturedRequests.Count.ShouldBeGreaterThan(afterSecond);
    }

    [Fact]
    public async Task SurviveOverlappingRefreshes()
    {
        AnswerGit(@"C:\repos\Northwind.Orders", root: "C:/repos/Northwind.Orders", commonDir: "C:/repos/Northwind.Orders/.git", branch: "main");
        IReadOnlyList<Session> sessions = [.. Enumerable.Range(0, 20).Select(_ => Session(@"C:\repos\Northwind.Orders"))];
        SessionRowEnricher enricher = Enricher;

        IReadOnlyList<SessionRow>[] results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => enricher.EnrichAsync(sessions, CancellationToken.None)));

        results.ShouldAllBe(rows => rows.Count == 20 && rows.All(row => row.Repository == "Northwind.Orders" && row.Head == new Head.OnBranch(BranchName.From("main"))));
    }

    [Fact]
    public async Task CarryTheCheckoutAndTheTabColour()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session inWorktree = Session(@"C:\repos\Northwind.Orders-fix");
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(inWorktree.SessionId), TabPalette.Default[2], Noon.AddMinutes(-5)).ClaimedBy(7001, Noon.AddMinutes(-5)));
        registry.Running(7001, Noon.AddMinutes(-5));
        Session outside = Session(@"C:\Users\me\scratch\notes");

        IReadOnlyList<SessionRow> rows = await Enricher.EnrichAsync([inWorktree, outside], CancellationToken.None);

        rows[0].Checkout.ShouldBe(FolderPath.From(@"C:\repos\Northwind.Orders"));
        rows[0].Tab.ShouldBe(new LiveTab.Open(TabPalette.Default[2]));
        rows[1].Checkout.ShouldBeNull();
        rows[1].Tab.ShouldBe(new LiveTab.None());
    }

    private void AnswerGit(string directory, string root, string commonDir, string branch)
    {
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-toplevel"), new ProcessResult(0, root + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--git-common-dir"), new ProcessResult(0, commonDir + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-current"), new ProcessResult(0, branch + "\n", string.Empty));
    }

    private static BackgroundSession Session(string workingDirectory)
    {
        return new BackgroundSession(SessionId.From(Guid.NewGuid().ToString()), JobId.From("81cdb0f9"), "name", FolderPath.From(workingDirectory), Noon, DisplayState.Working);
    }

    private static AutomatedRun Run(int processId)
    {
        return new AutomatedRun(SessionId.From(Guid.NewGuid().ToString()), FolderPath.From(@"C:\repos\Northwind.Orders-fix"), processId);
    }

    public void Dispose()
    {
        catalogue.Dispose();
    }
}