using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>The leftover cases over an in-memory folder tree; the registered-worktree cases run against real git in WorktreeInventoryShould.</summary>
public sealed class WorktreeInventoryLeftoversShould
{
    private const string Root = @"C:\Users\me\source\repos";
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";

    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Path.Combine(Repository, ".git", "objects"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n", string.Empty));

    private WorktreeInventory Inventory => new(new GitCli(processes), folders, [Root]);

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
}