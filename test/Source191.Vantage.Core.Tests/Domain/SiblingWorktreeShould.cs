using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SiblingWorktreeShould
{
    private static readonly WorktreeSlug Slug = WorktreeSlug.From("fix 123")!;

    [Fact]
    public void SitBesideTheRepositoryNamedAfterItAndTheSlug()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }

    [Fact]
    public void CheckOutABranchNamedAfterTheSlug()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Branch.ShouldBe(new BranchName("fix-123"));
    }

    [Fact]
    public void RefuseARepositoryAtADriveRootBecauseItHasNoParentToSitBeside()
    {
        FolderPath repository = new(@"C:\");

        ArgumentException refusal = Should.Throw<ArgumentException>(() => SiblingWorktree.For(repository, Slug));

        refusal.Message.ShouldStartWith(@"C:\ has no parent folder for a sibling worktree to sit in.");
    }

    [Fact]
    public void IgnoreATrailingSeparatorOnTheFolderPath()
    {
        FolderPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders\");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }
}