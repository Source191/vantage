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
        WorktreeRemovalOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls(processes).Single(request => request.Arguments.Contains("remove")).ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Repository),
            request => request.Arguments.ShouldBe(["worktree", "remove", Worktree]));
        outcome.Message.ShouldBe("Removed Northwind.Orders-fix-123; branch fix-123 was not created by Vantage, so it is kept.");
        outcome.BranchToConfirm.ShouldBe(new BranchToConfirm(Repository, "fix-123"));
    }

    [Fact]
    public async Task DeleteTheBranchWhenVantageCreatedItAndItIsMerged()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));

        WorktreeRemovalOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Ok), forced: false, CancellationToken.None);

        GitCalls(processes).Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        outcome.Message.ShouldBe("Removed Northwind.Orders-fix-123 and deleted its merged branch fix-123.");
        outcome.BranchToConfirm.ShouldBeNull();
        (await store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RefuseWhenTheRulesSayNoAndTouchNothing()
    {
        WorktreeRow dirty = Row(WorktreeHealthKind.Ok) with { DirtyFiles = 3 };

        WorktreeRemovalOutcome outcome = await Handler.RemoveAsync(dirty, forced: false, CancellationToken.None);

        outcome.Message.ShouldBe("Not removed: 3 changed or untracked files would be lost.");
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

        WorktreeRemovalOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Broken, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls(processes).ShouldContain(request => request.Arguments.Contains("prune"));
        outcome.Message.ShouldBe("Deleted the folder Northwind.Orders-stale and pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public async Task DeleteAnOrphanFolderWithoutTouchingGit()
    {
        string folder = Path.Combine(Root, "fix-service-tests");
        folders.WithDirectory(folder);

        WorktreeRemovalOutcome outcome = await Handler.RemoveAsync(Row(WorktreeHealthKind.Orphan, folder, branch: null), forced: false, CancellationToken.None);

        folders.DirectoryExists(folder).ShouldBeFalse();
        GitCalls(processes).ShouldBeEmpty();
        outcome.Message.ShouldBe("Deleted the folder fix-service-tests.");
    }
}