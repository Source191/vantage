using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SessionRowEnricherShould
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
        catalogue = new PullRequestCatalogue(new GitCli(remotes), hosts, clock);
    }

    private SessionRowEnricher Enricher => new(new GitCli(processes), store, registry, clock, transcripts, catalogue);

    [Fact]
    public async Task CarryEachRowsPullRequest()
    {
        PullRequest open = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, Noon);
        hosts.Answer(new GitHubRepository("northwind", "Northwind.Orders"), new PullRequestListing.Listed([new BranchPullRequest(new BranchName("fix"), open)]));
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session session = Session(@"C:\repos\Northwind.Orders-fix");
        SessionRowEnricher enricher = Enricher;

        SessionRow first = (await enricher.EnrichAsync([session], CancellationToken.None)).Single();
        await catalogue.RefreshStaleAsync([new FolderPath(@"C:\repos\Northwind.Orders")]);
        SessionRow second = (await enricher.EnrichAsync([session], CancellationToken.None)).Single();

        first.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
        second.PullRequest.ShouldBe(new PullRequestLookup.Found(open));
        hosts.Calls.Count.ShouldBe(1);
        remotes.CapturedRequests.Single().WorkingDirectory.ShouldBe(@"C:\repos\Northwind.Orders");
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
    public async Task SayWhetherASessionHasATranscriptToResumeFrom()
    {
        Session withMessages = Session(@"C:\repos\Northwind.Orders");
        Session untouched = Session(@"C:\repos\Northwind.Orders") with { SessionId = new SessionId("never-sent-anything") };
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
        row.Branch.ShouldBe(new BranchName("feature/x"));
        row.IsWorktree.ShouldBeFalse();
    }

    [Fact]
    public async Task NameTheParentRepositoryOfASessionInALinkedWorktree()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session session = Session(@"C:\repos\Northwind.Orders-fix");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("Northwind.Orders");
        row.Branch.ShouldBe(new BranchName("fix"));
        row.IsWorktree.ShouldBeTrue();
    }

    [Fact]
    public async Task FallBackToTheFolderNameOutsideAnyRepository()
    {
        Session session = Session(@"C:\Users\me\source\repos");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("repos");
        row.Branch.ShouldBeNull();
        row.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
        row.IsWorktree.ShouldBeFalse();
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

        row.HasLiveTab.ShouldBeTrue();
        (await store.ReadTabsAsync()).Count.ShouldBe(1, "the stale registration is removed on read");
    }

    [Fact]
    public async Task NotTakeVantagesOwnTabForASessionsTab()
    {
        Session session = Session(@"C:\repos\Northwind.Orders");
        await store.WriteTabAsync(TabRegistration.Pending(TabOwner.Vantage, TabPalette.Default[4], Noon.AddMinutes(-5)).ClaimedBy(7001, Noon.AddMinutes(-5)));
        registry.Running(7001, Noon.AddMinutes(-5));

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.HasLiveTab.ShouldBeFalse();
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

        results.ShouldAllBe(rows => rows.Count == 20 && rows.All(row => row.Repository == "Northwind.Orders" && row.Branch == new BranchName("main")));
    }

    private void AnswerGit(string directory, string root, string commonDir, string branch)
    {
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-toplevel"), new ProcessResult(0, root + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--git-common-dir"), new ProcessResult(0, commonDir + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-current"), new ProcessResult(0, branch + "\n", string.Empty));
    }

    private static BackgroundSession Session(string workingDirectory)
    {
        return new BackgroundSession(new SessionId(Guid.NewGuid().ToString()), new JobId("81cdb0f9"), "name", new FolderPath(workingDirectory), Noon, DisplayState.Working);
    }
}