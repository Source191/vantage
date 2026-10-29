using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class DeleteSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly InMemoryState store = new();
    private readonly FakeProcessRegistry registry = new();
    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Worktree);

    private DeleteSessionHandler Handler => new(new ClaudeCli(processes, Claude), new GitCli(processes, "git"), store, new SessionTabs(store, registry, new FakeTimeProvider(Noon)), folders);

    [Fact]
    public async Task DeleteASessionOutsideAVantageWorktreeWithoutTouchingGit()
    {
        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Repository), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["rm", "81cdb0f9"]);
        processes.CapturedRequests.ShouldNotContain(request => request.FileName == "git");
        outcome.ShouldBe(new DeleteSessionOutcome.Deleted("fix-123"));
    }

    [Fact]
    public async Task DeleteASessionAndRemoveItsCleanVantageWorktreeAndMergedBranch()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.Arguments.Contains("remove")).Arguments.ShouldBe(["worktree", "remove", Worktree]);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        (await store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
        outcome.ShouldBe(new DeleteSessionOutcome.RemovedWorktreeAndBranch("fix-123", BranchName.From("fix-123")));
    }

    [Fact]
    public async Task KeepAVantageWorktreeThatStillHasChanges()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M a.cs\n", string.Empty));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("remove"));
        outcome.ShouldBeOfType<DeleteSessionOutcome.KeptWorktree>().Reason.ShouldBe(new RemovalReason.ChangesWouldBeLost(1));
    }

    [Fact]
    public async Task AskAboutTheBranchWhenItIsNotMerged()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(1, string.Empty, string.Empty));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("remove"));
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("branch"));
        outcome.ShouldBe(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(FolderPath.From(Repository), BranchName.From("fix-123"))));
    }

    [Fact]
    public async Task DeleteABranchOnRequestWithForce()
    {
        DeleteBranchHandler handler = new(new GitCli(processes, "git"));

        Outcome message = await handler.HandleAsync(new DeleteBranch(new BranchToConfirm(FolderPath.From(Repository), BranchName.From("fix-123"))), CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(["branch", "-D", "fix-123"]);
        message.ShouldBe(new DeleteBranchOutcome());
    }

    [Fact]
    public async Task CloseTheTabBeforeTouchingTheWorktree()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));
        await LiveTabAsync(store, registry, shell: 4000);
        List<string> order = [];
        registry.OnEndCleanly = shell => order.Add($"shell {shell} ended after {processes.CapturedRequests.Count(request => request.FileName == "git")} git calls");

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        order.ShouldBe(["shell 4000 ended after 0 git calls"]);
        outcome.ShouldBe(new DeleteSessionOutcome.RemovedWorktreeAndBranch("fix-123", BranchName.From("fix-123")) { TabClosed = true });
    }

    [Fact]
    public async Task DeleteABrokenWorktreesFolderAndPrune()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("--porcelain") && request.Arguments.Contains("list"), new ProcessResult(0, "worktree C:/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/repos/Northwind.Orders-fix-123\nHEAD b\nbranch refs/heads/fix-123\nprunable gitdir file points to non-existent location\n", string.Empty));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("status"));
        folders.DirectoryExists(Worktree).ShouldBeFalse();
        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("prune"));
        outcome.ShouldBeOfType<DeleteSessionOutcome.RemovedWorktree>().Worktree.ShouldBeOfType<RemoveWorktreeOutcome.DeletedFolderAndPruned>().FolderName.ShouldBe("Northwind.Orders-fix-123");
    }

    [Fact]
    public async Task ReportAWorktreeStillInUse()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(Worktree), BranchName.From("fix-123"), FolderPath.From(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("remove"), new ProcessResult(255, string.Empty, "error: failed to delete 'C:/repos/Northwind.Orders-fix-123': Permission denied\n"));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["rm", "81cdb0f9"]);
        outcome.ShouldBe(new DeleteSessionOutcome.WorktreeInUse("fix-123", "Northwind.Orders-fix-123"));
    }
}