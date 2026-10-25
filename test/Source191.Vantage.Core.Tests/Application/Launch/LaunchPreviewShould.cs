using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class LaunchPreviewShould
{
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", new FolderPath(@"C:\repos\Northwind.Orders"), IsRepository: true);
    private static readonly FolderPath WorktreeRoot = new(@"C:\Users\me\source\worktrees");
    private static readonly RepositoryChoice NoRepository = new("repos (no repository)", new FolderPath(@"C:\repos"), IsRepository: false);

    [Fact]
    public void SayTheSessionRunsOnTheCurrentBranchWhenNoWorktreeIsWanted()
    {
        LaunchPreview.Describe(Repository, "Fix #123: flaky tests!", LaunchMode.CurrentBranch, WorktreeRoot)
            .ShouldBe("Runs in Northwind.Orders on its current branch; the name is only the session's label.");
    }

    [Fact]
    public void NameTheFolderUnderTheWorktreeRoot()
    {
        LaunchPreview.Describe(Repository, "Fix #123: flaky tests!", LaunchMode.NewWorktree, WorktreeRoot)
            .ShouldBe(@"Worktree Northwind.Orders-fix-123-flaky-tests under C:\Users\me\source\worktrees, on branch fix-123-flaky-tests.");
    }

    [Fact]
    public void ExplainWhatTheNameWillBecomeBeforeOneIsTyped()
    {
        LaunchPreview.Describe(Repository, "  ", LaunchMode.NewWorktree, WorktreeRoot)
            .ShouldBe(@"The name becomes the worktree folder Northwind.Orders-<name> under C:\Users\me\source\worktrees and its branch.");
    }

    [Fact]
    public void WarnThatAWorktreeNeedsARepository()
    {
        LaunchPreview.Describe(NoRepository, "anything", LaunchMode.NewWorktree, WorktreeRoot)
            .ShouldBe("A worktree needs a repository; this folder is not one.");
    }
}