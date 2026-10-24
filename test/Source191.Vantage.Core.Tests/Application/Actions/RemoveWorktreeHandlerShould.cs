using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class RemoveWorktreeHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeFolders folders = new();
    private readonly InMemoryState store = new();

    private RemoveWorktreeHandler Handler => new(new GitCli(processes), store, folders);

    [Fact]
    public async Task RemoveACleanWorktreeAndOfferToDeleteAHandMadeBranch()
    {
        RemoveWorktreeOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls(processes).Single(request => request.Arguments.Contains("remove")).ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Repository),
            request => request.Arguments.ShouldBe(["worktree", "remove", Worktree]));
        outcome.ShouldBe(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", new BranchToConfirm(Repository, "fix-123"), CreatedByVantage: false));
    }

    [Fact]
    public async Task DeleteTheBranchWhenVantageCreatedItAndItIsMerged()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));

        RemoveWorktreeOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls(processes).Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        outcome.ShouldBe(new RemoveWorktreeOutcome.RemovedAndDeletedBranch("Northwind.Orders-fix-123", "fix-123"));
        (await store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RefuseWhenTheRulesSayNoAndTouchNothing()
    {
        WorktreeRow dirty = Row(WorktreeHealthKind.Ok) with { DirtyFiles = 3 };

        RemoveWorktreeOutcome outcome = await Handler.RemoveAsync(dirty, forced: false, CancellationToken.None);

        outcome.ShouldBe(new RemoveWorktreeOutcome.Refused("3 changed or untracked files would be lost"));
        GitCalls(processes).ShouldNotContain(request => request.Arguments.Contains("remove"));
    }

    [Fact]
    public async Task ForceThroughGitWhenAsked()
    {
        WorktreeRow dirty = Row(WorktreeHealthKind.Ok) with { DirtyFiles = 3 };

        await Handler.RemoveAsync(dirty, forced: true, CancellationToken.None);

        GitCalls(processes).Single(request => request.Arguments.Contains("remove")).Arguments.ShouldBe(["worktree", "remove", "--force", Worktree]);
    }

    [Fact]
    public async Task DeleteABrokenLeftoverFolderAndPruneItsOwner()
    {
        string folder = Path.Combine(Root, "Northwind.Orders-stale");
        folders.WithFile(Path.Combine(folder, "pack.idx"));
        processes.AnswerInTurn(request => request.Arguments.Contains("--porcelain"), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n\nworktree C:/b\nHEAD b\nbranch refs/heads/x\n", string.Empty), new ProcessResult(0, "worktree C:/a\nHEAD a\nbranch refs/heads/main\n", string.Empty));

        RemoveWorktreeOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Broken, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls(processes).ShouldContain(request => request.Arguments.Contains("prune"));
        outcome.ShouldBe(new RemoveWorktreeOutcome.DeletedFolderAndPruned("Northwind.Orders-stale", 1, "Northwind.Orders"));
    }

    [Fact]
    public async Task DeleteAnOrphanFolderWithoutTouchingGit()
    {
        string folder = Path.Combine(Root, "fix-service-tests");
        folders.WithDirectory(folder);

        RemoveWorktreeOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Orphan, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls(processes).ShouldBeEmpty();
        outcome.ShouldBe(new RemoveWorktreeOutcome.DeletedFolder("fix-service-tests"));
    }
}