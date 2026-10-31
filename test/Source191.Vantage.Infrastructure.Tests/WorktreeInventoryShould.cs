using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Scans real repositories under a throwaway root with the real git.</summary>
[Trait("Category", "Integration")]
public sealed class WorktreeInventoryShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly TemporaryGitRoot root = new();

    private readonly FakePullRequests hosts = new();
    private readonly FakeProcessRunner remotes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, "https://github.com/northwind/Northwind.Orders.git", string.Empty));
    private readonly PullRequestCatalogue catalogue;

    public WorktreeInventoryShould()
    {
        // The origin comes from a fake: a real git still running in the folder in the background would stop Dispose deleting it.
        catalogue = new PullRequestCatalogue(new GitCli(remotes, "git"), hosts, TimeProvider.System);
    }

    private WorktreeInventory Inventory => new(new GitCli(new ProcessRunner(), "git"), new Folders(), TestSettings.Current(TestSettings.For([root.Path], root.WorktreeRoot)), catalogue);

    [Fact]
    public async Task CarryEachRowsPullRequest()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        await root.AddSiblingWorktreeAsync(repository, "fix-1");
        PullRequest open = new(PullRequestHost.GitHub, 7, new Uri("https://github.com/northwind/Northwind.Orders/pull/7"), PullRequestState.Open, Noon);
        hosts.Answer(new GitHubRepository("northwind", "Northwind.Orders"), new PullRequestListing.Listed([new BranchPullRequest(BranchName.From("fix-1"), open)]));

        WorktreeRow first = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();
        await catalogue.RefreshStaleAsync([(FolderPath.From(repository), BranchName.From("fix-1"))]);
        WorktreeRow second = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        first.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.Looking));
        second.PullRequest.ShouldBe(new PullRequestLookup.Found(open));
        hosts.Calls.Count.ShouldBe(1);
        remotes.CapturedRequests.ShouldAllBe(request => request.WorkingDirectory == repository);
    }

    [Fact]
    public async Task LookUpOnlyTheBranchesWithRows()
    {
        string orders = await root.CreateRepositoryAsync("Northwind.Orders");
        string billing = await root.CreateRepositoryAsync("Northwind.Billing");
        string[] idles = ["Northwind.Sales", "Northwind.Shipping", "Northwind.Catalogue"];
        foreach (string idle in idles)
        {
            await root.CreateRepositoryAsync(idle);
        }

        await root.AddSiblingWorktreeAsync(orders, "fix-1");
        await root.AddSiblingWorktreeAsync(billing, "fix-2");
        await root.GitAsync(orders, "worktree", "add", "--detach", $"{orders}-spike", "main");

        await Inventory.ScanAsync([], TestContext.Current.CancellationToken);
        while (hosts.Requests.Count < 2)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);
        hosts.Requests.Select(request => request.Branch.Value).ShouldBe(["fix-1", "fix-2"], ignoreOrder: true);
    }

    /// <summary>Plain claude running in the folder, or a stopped job there when it has no process.</summary>
    private static Session SessionIn(string directory, string name, int? processId)
    {
        SessionId id = SessionId.From(Guid.NewGuid().ToString());

        return processId is int running
            ? new InteractiveSession(id, name, FolderPath.From(directory), Noon, DisplayState.Idle, running)
            : new BackgroundSession(id, JobId.From("81cdb0f9"), name, FolderPath.From(directory), Noon, DisplayState.Stopped);
    }

    public void Dispose()
    {
        catalogue.Dispose();
        root.Dispose();
    }

    [Fact]
    public async Task ListTheLinkedWorktreesOfEveryRepositoryUnderTheRoots()
    {
        string first = await root.CreateRepositoryAsync("Northwind.First");
        string second = await root.CreateRepositoryAsync("Northwind.Second");
        string firstWorktree = await root.AddSiblingWorktreeAsync(first, "fix-1");
        string secondWorktree = await root.AddSiblingWorktreeAsync(second, "fix-2");

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Select(row => (row.RepositoryName, row.Path, row.Head, row.Health.Kind)).ShouldBe(
        [
            ("Northwind.First", FolderPath.From(firstWorktree), new Head.OnBranch(BranchName.From("fix-1")), WorktreeHealthKind.Ok),
            ("Northwind.Second", FolderPath.From(secondWorktree), new Head.OnBranch(BranchName.From("fix-2")), WorktreeHealthKind.Ok),
        ]);
    }

    [Fact]
    public async Task ReadAWorktreeOnADetachedHeadAsDetached()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = $"{repository}-spike";
        await root.GitAsync(repository, "worktree", "add", "--detach", worktree, "main");

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.ShouldBe(FolderPath.From(worktree));
        row.Head.ShouldBe(new Head.Detached());
        row.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch));
    }

    [Fact]
    public async Task ListWorktreesInBothPlacements()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string beside = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        string under = await root.AddWorktreeUnderTheWorktreeRootAsync(repository, "fix-2");

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Select(row => (row.Path, row.Health.Kind)).ShouldBe([(FolderPath.From(beside), WorktreeHealthKind.Ok), (FolderPath.From(under), WorktreeHealthKind.Ok)], ignoreOrder: true);
    }

    [Fact]
    public async Task ReportAFolderUnderClaudeWorktreesThatGitNoLongerListsAsAnOrphan()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string orphan = Path.Combine(repository, ".claude", "worktrees", "fix-service-tests");
        Directory.CreateDirectory(orphan);
        await File.WriteAllTextAsync(Path.Combine(orphan, "Program.cs"), "// left behind", TestContext.Current.CancellationToken);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(orphan);
        row.RepositoryName.ShouldBe("Northwind.Orders");
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        row.LeftoverFileCount.ShouldBe(1);
    }

    [Fact]
    public async Task StopCountingLeftoverFilesAtTheLimit()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string orphan = Path.Combine(repository, ".claude", "worktrees", "fix-service-tests", "node_modules");
        Directory.CreateDirectory(orphan);
        foreach (int number in Enumerable.Range(1, WorktreeRemovalFacts.LeftoverFileLimit + 5))
        {
            await File.WriteAllTextAsync(Path.Combine(orphan, $"{number}.js"), string.Empty, TestContext.Current.CancellationToken);
        }

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.LeftoverFileCount.ShouldBe(WorktreeRemovalFacts.LeftoverFileLimit);
    }

    [Fact]
    public async Task ReportASiblingWhoseRepositoryHasGoneAsBrokenWithNoOwnerToPrune()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Retired");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "republish");
        TemporaryGitRoot.DeleteTree(repository);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(worktree);
        row.RepositoryName.ShouldBe("Northwind.Retired");
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.OwnerExists.ShouldBeFalse();
        row.Head.ShouldBe(new Head.Unknown());
    }

    [Fact]
    public async Task ReportARegistrationWhoseFolderWasDeletedAsBrokenWithAnOwnerToPrune()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        TemporaryGitRoot.DeleteTree(worktree);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(worktree);
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.OwnerExists.ShouldBeTrue();
        row.DirtyFiles.ShouldBe(0);
    }

    [Fact]
    public async Task ReportEmptyLeftoverFoldersDirectlyUnderARoot()
    {
        Directory.CreateDirectory(Path.Combine(root.Path, "Northwind.Orders.worktrees"));
        Directory.CreateDirectory(Path.Combine(root.Path, "Archive.worktrees"));
        Directory.CreateDirectory(Path.Combine(root.Path, "just-a-folder"));

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Select(row => (row.Path.Name, row.Health.Kind)).ShouldBe(
        [
            ("Archive.worktrees", WorktreeHealthKind.Empty),
            ("Northwind.Orders.worktrees", WorktreeHealthKind.Empty),
        ]);
    }

    [Fact]
    public async Task MarkALockedWorktreeWithItsReason()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        await root.GitAsync(repository, "worktree", "lock", "--reason", "another tool is using it", worktree);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Health.ShouldBe(new WorktreeHealth(WorktreeHealthKind.Locked, new HealthReason.Locked("another tool is using it")));
    }

    [Fact]
    public async Task CountTheWorkAtRiskInARegisteredWorktree()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        await root.CommitAFileAsync(worktree, "second.txt");
        foreach (int number in Enumerable.Range(1, 5))
        {
            await File.WriteAllTextAsync(Path.Combine(worktree, $"dirty-{number}.txt"), "x", TestContext.Current.CancellationToken);
        }

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.DirtyFiles.ShouldBe(5);
        row.UnpushedCommits.ShouldBe(2);
        row.LastCommitAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task NameTheLiveSessionWorkingInsideAWorktree()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        Session live = SessionIn(Path.Combine(worktree, "src"), "fix-the-tests", processId: 4242);
        Session dead = SessionIn(worktree, "finished-earlier", processId: null);
        Session elsewhere = SessionIn(repository, "main-work", processId: 4343);

        WorktreeRow row = (await Inventory.ScanAsync([elsewhere, dead, live], TestContext.Current.CancellationToken)).Single();

        row.Session.ShouldBe(new WorktreeSession.Running(live.SessionId, "fix-the-tests"));
    }

    [Fact]
    public async Task ReportTheSessionIdOfTheLiveSession()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        Session live = SessionIn(worktree, "fix-tests", processId: 4242);
        Session sameName = SessionIn(repository, "fix-tests", processId: 4343);

        WorktreeRow row = (await Inventory.ScanAsync([sameName, live], TestContext.Current.CancellationToken)).Single();

        row.Session.ShouldBe(new WorktreeSession.Running(live.SessionId, "fix-tests"));
    }

    [Fact]
    public async Task KeepScanningWhenOneFolderIsNotAUsableRepository()
    {
        Directory.CreateDirectory(Path.Combine(root.Path, "Corrupt", ".git"));
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Single().Path.Value.ShouldBe(worktree);
    }
}