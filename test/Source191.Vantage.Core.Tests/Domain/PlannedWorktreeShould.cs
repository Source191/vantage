using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class PlannedWorktreeShould
{
    private static readonly WorktreeSlug Slug = WorktreeSlug.From("fix 123")!;
    private static readonly FolderPath WorktreeRoot = new(@"C:\Users\me\source\worktrees");

    [Fact]
    public void PlaceTheWorktreeUnderTheWorktreeRoot()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        PlannedWorktree worktree = PlannedWorktree.For(WorktreeRoot, repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");
    }

    [Fact]
    public void CheckOutABranchNamedAfterTheSlug()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        PlannedWorktree worktree = PlannedWorktree.For(WorktreeRoot, repository, Slug);

        worktree.Branch.ShouldBe(new BranchName("fix-123"));
    }

    [Fact]
    public void IgnoreATrailingSeparatorOnTheRepositoryPath()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders\");

        PlannedWorktree worktree = PlannedWorktree.For(WorktreeRoot, repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");
    }

    [Fact]
    public void PlaceTheWorktreeOnAnotherDrive()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        PlannedWorktree worktree = PlannedWorktree.For(new FolderPath(@"D:\worktrees"), repository, Slug);

        worktree.Path.Value.ShouldBe(@"D:\worktrees\Northwind.Orders-fix-123");
    }
}
