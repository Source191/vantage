using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Application;

public sealed class SessionRowEnricherShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string stateFolder = Path.Combine(Path.GetTempPath(), "vantage-enrich-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(128, string.Empty, "fatal: not a git repository"));
    private readonly FakeProcessRegistry registry = new();
    private readonly FakeTimeProvider clock = new(Noon);

    private readonly FakeTranscripts transcripts = new();

    private SessionRowEnricher Enricher => new(new GitCli(processes), new StateStore(stateFolder, clock), registry, clock, transcripts);

    [Fact]
    public async Task SayWhetherASessionHasATranscriptToResumeFrom()
    {
        Session withMessages = Session(@"C:\repos\Northwind.Orders");
        Session untouched = Session(@"C:\repos\Northwind.Orders") with { SessionId = "never-sent-anything" };
        transcripts.WithMessages(withMessages.SessionId);

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
        row.Branch.ShouldBe("feature/x");
        row.IsWorktree.ShouldBeFalse();
    }

    [Fact]
    public async Task NameTheParentRepositoryOfASessionInALinkedWorktree()
    {
        AnswerGit(@"C:\repos\Northwind.Orders-fix", root: "C:/repos/Northwind.Orders-fix", commonDir: "C:/repos/Northwind.Orders/.git", branch: "fix");
        Session session = Session(@"C:\repos\Northwind.Orders-fix");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("Northwind.Orders");
        row.Branch.ShouldBe("fix");
        row.IsWorktree.ShouldBeTrue();
    }

    [Fact]
    public async Task FallBackToTheFolderNameOutsideAnyRepository()
    {
        Session session = Session(@"C:\Users\me\source\repos");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Repository.ShouldBe("repos");
        row.Branch.ShouldBeNull();
        row.IsWorktree.ShouldBeFalse();
    }

    [Fact]
    public async Task AttachTheStoredDescription()
    {
        Session session = Session(@"C:\repos\Northwind.Orders");
        await new StateStore(stateFolder, clock).WriteDescriptionAsync(session.SessionId, "Replace the hook");

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.Description.ShouldBe("Replace the hook");
    }

    [Fact]
    public async Task MarkASessionWithALiveVantageTab()
    {
        Session session = Session(@"C:\repos\Northwind.Orders");
        TabRegistration live = TabRegistration.Pending(session.SessionId, TabPalette.Default[0], Noon.AddMinutes(-5)).ClaimedBy(7001, Noon.AddMinutes(-5));
        TabRegistration stale = TabRegistration.Pending(session.SessionId, TabPalette.Default[1], Noon.AddHours(-1));
        await new StateStore(stateFolder, clock).WriteTabAsync(live);
        await new StateStore(stateFolder, clock).WriteTabAsync(stale);
        registry.Running(7001, Noon.AddMinutes(-5));

        SessionRow row = (await Enricher.EnrichAsync([session], CancellationToken.None)).Single();

        row.HasLiveTab.ShouldBeTrue();
        (await new StateStore(stateFolder, clock).ReadTabsAsync()).Count.ShouldBe(1, "the stale registration is removed on read");
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

        results.ShouldAllBe(rows => rows.Count == 20 && rows.All(row => row.Repository == "Northwind.Orders" && row.Branch == "main"));
    }

    public void Dispose()
    {
        if (Directory.Exists(stateFolder))
        {
            Directory.Delete(stateFolder, recursive: true);
        }
    }

    private void AnswerGit(string directory, string root, string commonDir, string branch)
    {
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-toplevel"), new ProcessResult(0, root + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--git-common-dir"), new ProcessResult(0, commonDir + "\n", string.Empty));
        processes.Answer(request => request.WorkingDirectory == directory && request.Arguments.Contains("--show-current"), new ProcessResult(0, branch + "\n", string.Empty));
    }

    private static Session Session(string workingDirectory)
    {
        return new Session(Guid.NewGuid().ToString(), "job1", "name", workingDirectory, SessionKind.Background, Noon, DisplayState.Working);
    }
}