using Source191.Vantage.Actions;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Screens;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Actions;

public sealed class WorktreeActionsShould : IDisposable
{
    private const string Repository = @"C:\repos\Northwind.Orders";
    private const string Worktree = @"C:\repos\Northwind.Orders-fix-123";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string scratch = Path.Combine(Path.GetTempPath(), "vantage-worktree-actions-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
        .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeWindowsTerminal terminal = new();

    private StateStore Store => new(Path.Combine(scratch, "state"));

    private WorktreeActions Actions => new(new GitCli(processes), Store, terminal);

    private IEnumerable<ProcessRequest> GitCalls => processes.CapturedRequests.Where(request => request.FileName == "git");

    private static WorktreeRow Row(WorktreeHealthKind health, string path = Worktree, string? branch = "fix-123", bool ownerExists = true)
    {
        return new WorktreeRow(Repository, path, branch, new WorktreeHealth(health, string.Empty), 0, 0, Noon, null, null, 0, ownerExists, FolderExists: true);
    }

    public void Dispose()
    {
        TemporaryGitRoot.DeleteTree(scratch);
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
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls.Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        outcome.Message.ShouldBe("Removed Northwind.Orders-fix-123 and deleted its merged branch fix-123.");
        outcome.BranchToConfirm.ShouldBeNull();
        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
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
        string folder = Path.Combine(scratch, "Northwind.Orders-stale");
        Directory.CreateDirectory(folder);
        string readOnly = Path.Combine(folder, "pack.idx");
        await File.WriteAllTextAsync(readOnly, "x", TestContext.Current.CancellationToken);
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        processes.AnswerInTurn(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n\nworktree C:/b\nHEAD b\nbranch refs/heads/x\n", string.Empty), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n", string.Empty));

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Broken, folder, branch: null), forced: false, CancellationToken.None);

        Directory.Exists(folder).ShouldBeFalse();
        GitCalls.ShouldContain(request => request.Arguments.Contains("prune"));
        outcome.Message.ShouldBe("Deleted the folder Northwind.Orders-stale and pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public async Task DeleteAnOrphanFolderWithoutTouchingGit()
    {
        string folder = Path.Combine(scratch, "fix-service-tests");
        Directory.CreateDirectory(folder);

        WorktreeRemovalOutcome outcome = await Actions.RemoveAsync(Row(WorktreeHealthKind.Orphan, folder, branch: null), forced: false, CancellationToken.None);

        Directory.Exists(folder).ShouldBeFalse();
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