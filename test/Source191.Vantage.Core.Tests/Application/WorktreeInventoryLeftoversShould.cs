using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>The leftover cases over an in-memory folder tree; the registered-worktree cases run against real git in WorktreeInventoryShould.</summary>
public sealed class WorktreeInventoryLeftoversShould : IDisposable
{
    private const string Root = @"C:\Users\me\source\repos";
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    private const string WorktreeRoot = @"C:\Users\me\source\worktrees";

    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Path.Combine(Repository, ".git", "objects"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n", string.Empty));

    private readonly PullRequestCatalogue catalogue = new(new GitCli(new FakeProcessRunner().AnswerAll(new ProcessResult(2, string.Empty, string.Empty)), "git"), new FakePullRequests(), TimeProvider.System);

    private WorktreeInventory Inventory => new(new GitCli(processes, "git"), folders, TestSettings.Current(Root), catalogue);

    [Fact]
    public async Task AskGitThreeThingsPerRegisteredWorktreeAndNothingAboutMergeState()
    {
        string worktree = Path.Combine(Root, "Northwind.Orders-fix-123");
        folders.WithDirectory(worktree);
        processes.Answer(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/Users/me/source/repos/Northwind.Orders-fix-123\nHEAD b\nbranch refs/heads/fix-123\n", string.Empty));

        processes.Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "0\n", string.Empty));

        await Inventory.ScanAsync([], CancellationToken.None);

        processes.CapturedRequests.Where(request => request.WorkingDirectory == worktree).Select(request => request.Arguments[0]).ShouldBe(["status", "rev-list", "log"]);
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("merge-base") || request.Arguments.Contains("symbolic-ref"));
    }

    [Fact]
    public async Task ReportAFolderUnderClaudeWorktreesThatGitNoLongerListsAsAnOrphanWithItsFileCount()
    {
        string orphan = Path.Combine(Repository, ".claude", "worktrees", "fix-service-tests");
        folders.WithFile(Path.Combine(orphan, "Program.cs")).WithFile(Path.Combine(orphan, "src", "Tests.cs"));

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.Path.Value.ShouldBe(orphan);
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        row.LeftoverFileCount.ShouldBe(2);
        row.OwnerExists.ShouldBeTrue();
        row.PullRequest.ShouldBe(new PullRequestLookup.Missing(PullRequestGap.NoBranch), "a leftover has no branch to look up");
    }

    [Fact]
    public async Task ReportAnOrphanWhosePointerNamesAGoneRegistrationAsBrokenWithoutCountingThePointer()
    {
        string orphan = Path.Combine(Repository, ".claude", "worktrees", "fix-service-tests");
        folders.WithFile(Path.Combine(orphan, "Program.cs")).WithFile(Path.Combine(orphan, ".git"), "gitdir: C:/gone");

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.LeftoverFileCount.ShouldBe(1, "the .git pointer file does not count");
    }

    [Fact]
    public async Task ReportAnEmptyWorktreesFolderDirectlyUnderARootAsEmpty()
    {
        folders.WithDirectory(Path.Combine(Root, "Northwind.Orders.worktrees")).WithDirectory(Path.Combine(Root, "just-a-folder"));

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.FolderName.ShouldBe("Northwind.Orders.worktrees");
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Empty);
        row.Repository.Value.ShouldBe(Root);
    }

    [Fact]
    public async Task ReportASiblingWhosePointerNamesAGoneRegistrationAsBrokenWithItsOwner()
    {
        string sibling = Path.Combine(Root, "Northwind.Orders-republish");
        folders.WithFile(Path.Combine(sibling, ".git"), "gitdir: C:/Users/me/source/repos/Northwind.Orders/.git/worktrees/republish\n").WithFile(Path.Combine(sibling, "notes.txt"));

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.Path.Value.ShouldBe(sibling);
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.Repository.Value.ShouldBe(Repository);
        row.OwnerExists.ShouldBeTrue();
        row.LeftoverFileCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReportAWorktreeOfARepositoryOutsideTheRootsAsElsewhere()
    {
        string worktree = Path.Combine(WorktreeRoot, "Northwind.Ledger-fix");
        folders.WithFile(Path.Combine(worktree, ".git"), "gitdir: D:/other/Northwind.Ledger/.git/worktrees/fix\n")
            .WithDirectory(@"D:\other\Northwind.Ledger\.git\worktrees\fix")
            .WithFile(Path.Combine(worktree, "notes.txt"));

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.Health.Kind.ShouldBe(WorktreeHealthKind.Elsewhere);
        row.Health.Reason.ShouldBe(new HealthReason.RepositoryOutsideRoots(FolderPath.From(@"D:\other\Northwind.Ledger")));
        row.Repository.Value.ShouldBe(@"D:\other\Northwind.Ledger");
        row.OwnerExists.ShouldBeTrue();
    }

    [Fact]
    public async Task FindABrokenWorktreeUnderTheWorktreeRoot()
    {
        string worktree = Path.Combine(WorktreeRoot, "Northwind.Orders-republish");
        folders.WithFile(Path.Combine(worktree, ".git"), "gitdir: C:/Users/me/source/repos/Northwind.Orders/.git/worktrees/republish\n");

        WorktreeRow row = (await Inventory.ScanAsync([], CancellationToken.None)).Single();

        row.Path.Value.ShouldBe(worktree);
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.Repository.Value.ShouldBe(Repository);
    }

    [Fact]
    public async Task NotLookForEmptyLeftoversUnderTheWorktreeRoot()
    {
        folders.WithDirectory(Path.Combine(WorktreeRoot, "Northwind.Orders.worktrees"));

        (await Inventory.ScanAsync([], CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task NotListAFolderTwiceWhenTheWorktreeRootIsARoot()
    {
        string sibling = Path.Combine(Root, "Northwind.Orders-republish");
        folders.WithFile(Path.Combine(sibling, ".git"), "gitdir: C:/Users/me/source/repos/Northwind.Orders/.git/worktrees/republish\n");
        WorktreeInventory inventory = new(new GitCli(processes, "git"), folders, TestSettings.Current(TestSettings.For([Root], worktreeRoot: Root)), catalogue);

        (await inventory.ScanAsync([], CancellationToken.None)).Select(row => row.Path.Value).ShouldBe([sibling]);
    }

    [Fact]
    public async Task ScanTheRootsSavedAfterItWasBuilt()
    {
        const string Work = @"D:\work";
        string sibling = Path.Combine(Work, "Northwind.Ledger-republish");
        folders.WithFile(Path.Combine(sibling, ".git"), "gitdir: D:/work/Northwind.Ledger/.git/worktrees/republish\n");
        CurrentSettings settings = TestSettings.Current(Root);
        WorktreeInventory inventory = new(new GitCli(processes, "git"), folders, settings, catalogue);

        await settings.SaveAsync(TestSettings.For(Root, Work));

        (await inventory.ScanAsync([], CancellationToken.None)).Select(row => row.Path.Value).ShouldBe([sibling]);
    }

    public void Dispose()
    {
        catalogue.Dispose();
    }
}