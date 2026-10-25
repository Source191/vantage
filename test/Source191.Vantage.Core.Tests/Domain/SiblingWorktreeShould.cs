using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SiblingWorktreeShould
{
    private static readonly WorktreeSlug Slug = WorktreeSlug.From("fix 123");

    [Fact]
    public void SitBesideTheRepositoryNamedAfterItAndTheSlug()
    {
        FolderPath repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }

    [Fact]
    public void CheckOutABranchNamedAfterTheSlug()
    {
        FolderPath repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Branch.ShouldBe(BranchName.From("fix-123"));
    }

    [Fact]
    public void RefuseARepositoryAtADriveRootBecauseItHasNoParentToSitBeside()
    {
        FolderPath repository = FolderPath.From(@"C:\");

        ArgumentException refusal = Should.Throw<ArgumentException>(() => SiblingWorktree.For(repository, Slug));

        refusal.Message.ShouldStartWith(@"C:\ has no parent folder for a sibling worktree to sit in.");
    }

    [Fact]
    public void IgnoreATrailingSeparatorOnTheFolderPath()
    {
        FolderPath repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders\");

        SiblingWorktree worktree = SiblingWorktree.For(repository, Slug);

        worktree.Path.Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders-fix-123");
    }
}