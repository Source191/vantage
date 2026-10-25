using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class FolderLabelsShould
{
    private static readonly FolderPath Home = new(@"C:\Users\me");
    private static readonly FolderPath Repos = new(@"C:\Users\me\source\repos");
    private static readonly FolderPath Worktrees = new(@"C:\Users\me\source\worktrees");

    [Fact]
    public void UseTheRepositoryRootsAndTheWorktreeRoot()
    {
        FolderLabels labels = new(() => [Repos, Worktrees], Home);

        labels.For(new FolderPath(@"C:\Users\me\source\repos\Northwind.Orders")).ShouldBe("Northwind.Orders");
        labels.For(new FolderPath(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123")).ShouldBe("Northwind.Orders-fix-123");
    }

    [Fact]
    public void FollowAChangedConfiguration()
    {
        List<FolderPath> roots = [Repos];
        FolderLabels labels = new(() => roots, Home);
        FolderPath worktree = new(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");

        labels.For(worktree).ShouldBe(@"~\source\worktrees\Northwind.Orders-fix-123");
        roots.Add(Worktrees);

        labels.For(worktree).ShouldBe("Northwind.Orders-fix-123");
    }
}