using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class WorktreeActionsShould
{
    private const string Root = @"C:\Users\me\source\repos";
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    private const string Worktree = @"C:\Users\me\source\repos\Northwind.Orders-fix-123";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
        .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeWindowsTerminal terminal = new();
    private readonly FakeFolders folders = new();
    private readonly InMemoryState store = new();

    private WorktreeActions Actions => new(new GitCli(processes), store, folders, terminal);

    private IEnumerable<ProcessRequest> GitCalls => processes.CapturedRequests.Where(request => request.FileName == "git");

    private static WorktreeRow Row(WorktreeHealthKind health, string path = Worktree, string? branch = "fix-123", bool ownerExists = true)
    {
        return new WorktreeRow(Repository, path, branch, new WorktreeHealth(health, string.Empty), 0, 0, Noon, null, null, 0, ownerExists, FolderExists: true);
    }

    [Fact]
    public async Task RemoveACleanWorktreeAndOfferToDeleteAHandMadeBranch()
    {
        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls.Single(request => request.Arguments.Contains("remove")).ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Repository),
            request => request.Arguments.ShouldBe(["worktree", "remove", Worktree]));
        outcome.Message.ShouldBe("Removed Northwind.Orders-fix-123; branch fix-123 was not created by Vantage, so it is kept.");
        outcome.BranchToConfirm.ShouldBe(new BranchToConfirm(Repository, "fix-123"));
    }

    [Fact]
    public async Task DeleteTheBranchWhenVantageCreatedItAndItIsMerged()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls.Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        outcome.Message.ShouldBe("Removed Northwind.Orders-fix-123 and deleted its merged branch fix-123.");
        outcome.BranchToConfirm.ShouldBeNull();
        (await store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RefuseWhenTheRulesSayNoAndTouchNothing()
    {
        WorktreeRow dirty = Row(WorktreeHealthKind.Ok) with { DirtyFiles = 3 };

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(dirty, forced: false, CancellationToken.None);

        outcome.Message.ShouldBe("Not removed: 3 changed or untracked files would be lost.");
        GitCalls.ShouldNotContain(request => request.Arguments.Contains("remove"));
    }

    [Fact]
    public async Task ForceThroughGitWhenAsked()
    {
        WorktreeRow dirty = Row(WorktreeHealthKind.Ok) with { DirtyFiles = 3 };

        await Actions.RemoveAsync(dirty, forced: true, CancellationToken.None);

        GitCalls.Single(request => request.Arguments.Contains("remove")).Arguments.ShouldBe(["worktree", "remove", "--force", Worktree]);
    }

    [Fact]
    public async Task DeleteABrokenLeftoverFolderAndPruneItsOwner()
    {
        string folder = Path.Combine(Root, "Northwind.Orders-stale");
        folders.WithFile(Path.Combine(folder, "pack.idx"));
        processes.AnswerInTurn(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n\nworktree C:/b\nHEAD b\nbranch refs/heads/x\n", string.Empty), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n", string.Empty));

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Broken, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls.ShouldContain(request => request.Arguments.Contains("prune"));
        outcome.Message.ShouldBe("Deleted the folder Northwind.Orders-stale and pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public async Task DeleteAnOrphanFolderWithoutTouchingGit()
    {
        string folder = Path.Combine(Root, "fix-service-tests");
        folders.WithDirectory(folder);

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Orphan, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls.ShouldBeEmpty();
        outcome.Message.ShouldBe("Deleted the folder fix-service-tests.");
    }

    [Fact]
    public async Task ReportHowManyRegistrationsAPruneRemoved()
    {
        processes.AnswerInTurn(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n\nworktree C:/b\nHEAD b\nbranch refs/heads/x\n\nworktree C:/c\nHEAD c\nbranch refs/heads/y\n", string.Empty), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n", string.Empty));

        string message = await Actions.PruneAsync(Repository, CancellationToken.None);

        message.ShouldBe("Pruned 2 stale registrations from Northwind.Orders.");
    }

    [Fact]
    public async Task OpenAPlainShellTabInTheWorktree()
    {
        string message = await Actions.OpenShellAsync(Row(WorktreeHealthKind.Ok), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Northwind.Orders-fix-123");
        tab.StartingDirectory.ShouldBe(Worktree);
        message.ShouldBe("Opened a shell in Northwind.Orders-fix-123.");
    }

    [Fact]
    public async Task DeleteABranchOnRequestWithForce()
    {
        await Actions.DeleteBranchAsync(new BranchToConfirm(Repository, "fix-123"), CancellationToken.None);

        GitCalls.Single().Arguments.ShouldBe(["branch", "-D", "fix-123"]);
    }
}