using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class FolderLabelsShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");
    private static readonly FolderPath Repos = FolderPath.From(@"C:\Users\me\source\repos");
    private static readonly FolderPath Worktrees = FolderPath.From(@"C:\Users\me\source\worktrees");

    [Fact]
    public void UseTheRepositoryRootsAndTheWorktreeRoot()
    {
        FolderLabels labels = new(TestSettings.Current(TestSettings.For([Repos.Value], Worktrees.Value)), Home);

        labels.For(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders")).ShouldBe("Northwind.Orders");
        labels.For(FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123")).ShouldBe("Northwind.Orders-fix-123");
    }

    [Fact]
    public async Task FollowASavedConfiguration()
    {
        CurrentSettings settings = TestSettings.Current(TestSettings.For([Repos.Value], @"D:\work"));
        FolderLabels labels = new(settings, Home);
        FolderPath worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");

        labels.For(worktree).ShouldBe(@"~\source\worktrees\Northwind.Orders-fix-123");
        await settings.SaveAsync(TestSettings.For([Repos.Value], Worktrees.Value));

        labels.For(worktree).ShouldBe("Northwind.Orders-fix-123");
    }
}