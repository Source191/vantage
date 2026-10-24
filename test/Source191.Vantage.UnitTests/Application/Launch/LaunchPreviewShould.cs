using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.UnitTests.Application.Launch;

public sealed class LaunchPreviewShould
{
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", @"C:\repos\Northwind.Orders", IsRepository: true);
    private static readonly RepositoryChoice NoRepository = new("repos (no repository)", @"C:\repos", IsRepository: false);

    [Fact]
    public void SayTheSessionRunsOnTheCurrentBranchWhenNoWorktreeIsWanted()
    {
        LaunchPreview.Describe(Repository, "Fix #123: flaky tests!", LaunchMode.CurrentBranch)
            .ShouldBe("Runs in Northwind.Orders on its current branch; the name is only the session's label.");
    }

    [Fact]
    public void ShowTheWorktreeFolderAndBranchTheNameBecomes()
    {
        LaunchPreview.Describe(Repository, "Fix #123: flaky tests!", LaunchMode.NewWorktree)
            .ShouldBe("Worktree Northwind.Orders-fix-123-flaky-tests beside the repository, on branch fix-123-flaky-tests.");
    }

    [Fact]
    public void ExplainWhatTheNameWillBecomeBeforeOneIsTyped()
    {
        LaunchPreview.Describe(Repository, "  ", LaunchMode.NewWorktree)
            .ShouldBe("The name becomes the worktree folder Northwind.Orders-<name> and its branch.");
    }

    [Fact]
    public void WarnThatAWorktreeNeedsARepository()
    {
        LaunchPreview.Describe(NoRepository, "anything", LaunchMode.NewWorktree)
            .ShouldBe("A worktree needs a repository; this folder is not one.");
    }
}