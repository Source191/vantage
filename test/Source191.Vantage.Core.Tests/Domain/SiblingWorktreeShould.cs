using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SiblingWorktreeShould
{
    private static readonly WorktreeSlug Slug = WorktreeSlug.From("fix 123")!;

    [Fact]
    public void SitBesideTheRepositoryNamedAfterItAndTheSlug()
    {
        RepositoryPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }

    [Fact]
    public void CheckOutABranchNamedAfterTheSlug()
    {
        RepositoryPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Branch.ShouldBe("fix-123");
    }

    [Fact]
    public void RefuseARepositoryAtADriveRootBecauseItHasNoParentToSitBeside()
    {
        RepositoryPath repository = new(@"C:\");

        ArgumentException refusal = Should.Throw<ArgumentException>(() => SiblingWorktree.For(repository, Slug));

        refusal.Message.ShouldStartWith(@"C:\ has no parent folder for a sibling worktree to sit in.");
    }

    [Fact]
    public void IgnoreATrailingSeparatorOnTheRepositoryPath()
    {
        RepositoryPath repository = new(@"C:\Users\me\source\repos\Northwind.Orders\");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }
}