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

    private DeleteSessionHandler Handler => new(new ClaudeCli(processes, Claude), new GitCli(processes), store);

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
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(new FolderPath(Worktree), new BranchName("fix-123"), new FolderPath(Repository), Noon));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.Arguments.Contains("remove")).Arguments.ShouldBe(["worktree", "remove", Worktree]);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        (await store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
        outcome.ShouldBe(new DeleteSessionOutcome.RemovedWorktreeAndBranch("fix-123", new BranchName("fix-123")));
    }

    [Fact]
    public async Task KeepAVantageWorktreeThatStillHasChanges()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(new FolderPath(Worktree), new BranchName("fix-123"), new FolderPath(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M a.cs\n", string.Empty));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("remove"));
        outcome.ShouldBeOfType<DeleteSessionOutcome.KeptWorktree>().Reason.ShouldContain("1 changed");
    }

    [Fact]
    public async Task AskAboutTheBranchWhenItIsNotMerged()
    {
        await store.RecordCreatedWorktreeAsync(new CreatedWorktree(new FolderPath(Worktree), new BranchName("fix-123"), new FolderPath(Repository), Noon));
        processes.Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(1, string.Empty, string.Empty));

        DeleteSessionOutcome outcome = await Handler.DeleteAsync(Job(Worktree), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("remove"));
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("branch"));
        outcome.ShouldBe(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(new FolderPath(Repository), new BranchName("fix-123"))));
    }

    [Fact]
    public async Task DeleteABranchOnRequestWithForce()
    {
        DeleteBranchHandler handler = new(new GitCli(processes));

        Outcome message = await handler.HandleAsync(new DeleteBranch(new BranchToConfirm(new FolderPath(Repository), new BranchName("fix-123"))), CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(["branch", "-D", "fix-123"]);
        message.ShouldBe(new DeleteBranchOutcome());
    }
}