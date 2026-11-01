using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WorktreeInspectorShould
{
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));

    private WorktreeInspector Inspector => new(new GitCli(processes, "git"));

    [Fact]
    public async Task GatherFactsForARegisteredWorktree()
    {
        processes
            .Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M a.cs\n?? b.cs\n", string.Empty))
            .Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "3\n", string.Empty))
            .Answer(request => request.Arguments.Contains("log"), new ProcessResult(0, "2026-10-23T12:00:00+00:00\n", string.Empty));

        WorktreeCounts counts = await Inspector.CountAsync(Worktree, CancellationToken.None);

        counts.ShouldBe(new WorktreeCounts(DirtyFiles: 2, UnpushedCommits: 3, LastCommitAt: new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero)));
        processes.CapturedRequests.ShouldAllBe(request => request.WorkingDirectory == new WorkingFolder.At(Worktree.Value));
    }

    [Fact]
    public async Task ReportBrokenWhenGitNoLongerListsIt()
    {
        processes.Answer(request => request.Arguments.Contains("list"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n", string.Empty));

        WorktreeHealth health = await Inspector.HealthAsync(Repository, Worktree, CancellationToken.None);

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
    }

    [Fact]
    public async Task ReportLockedWithTheReason()
    {
        processes.Answer(request => request.Arguments.Contains("list"), new ProcessResult(0, "worktree C:/Users/me/source/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/Users/me/source/repos/Northwind.Orders-fix-123\nHEAD b\nbranch refs/heads/fix-123\nlocked in use by another tool\n", string.Empty));

        WorktreeHealth health = await Inspector.HealthAsync(Repository, Worktree, CancellationToken.None);

        health.Kind.ShouldBe(WorktreeHealthKind.Locked);
        health.Reason.ShouldBe(new HealthReason.Locked("in use by another tool"));
    }

    [Fact]
    public async Task AskWhetherTheBranchIsMergedIntoTheRemoteDefault()
    {
        processes.Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty));

        bool merged = await Inspector.IsMergedAsync(Repository, BranchName.From("fix-123"), CancellationToken.None);

        merged.ShouldBeTrue();
        processes.CapturedRequests.Single(request => request.Arguments.Contains("merge-base")).Arguments.ShouldBe(["merge-base", "--is-ancestor", "fix-123", "origin/main"]);
    }

    [Fact]
    public async Task FallBackToHeadWhenThereIsNoRemoteDefault()
    {
        processes
            .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(128, string.Empty, "fatal: ref refs/remotes/origin/HEAD is not a symbolic ref"))
            .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(1, string.Empty, string.Empty));

        bool merged = await Inspector.IsMergedAsync(Repository, BranchName.From("fix-123"), CancellationToken.None);

        merged.ShouldBeFalse();
        processes.CapturedRequests.Single(request => request.Arguments.Contains("merge-base")).Arguments[^1].ShouldBe("HEAD");
    }
}