using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class FolderLabelShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");
    private static readonly FolderPath Repos = FolderPath.From(@"C:\Users\me\source\repos");
    private static readonly FolderPath Worktrees = FolderPath.From(@"C:\Users\me\source\worktrees");

    [Fact]
    public void ShowAFolderRelativeToTheRootThatHoldsIt()
    {
        FolderLabel.For(FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123"), [Repos, Worktrees], Home).ShouldBe("Northwind.Orders-fix-123");
        FolderLabel.For(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders\src\Api"), [Repos, Worktrees], Home).ShouldBe(@"Northwind.Orders\src\Api");
    }

    [Fact]
    public void PreferTheDeepestRoot()
    {
        FolderPath nested = FolderPath.From(@"C:\Users\me\source\repos\worktrees");
        FolderPath folder = FolderPath.From(@"C:\Users\me\source\repos\worktrees\Northwind.Orders-fix-123");

        FolderLabel.For(folder, [Repos, nested], Home).ShouldBe("Northwind.Orders-fix-123");
        FolderLabel.For(folder, [nested, Repos], Home).ShouldBe("Northwind.Orders-fix-123");
    }

    [Fact]
    public void ShowAFolderOutsideEveryRootRelativeToHome()
    {
        FolderLabel.For(FolderPath.From(@"C:\Users\me\scratch\notes"), [Repos, Worktrees], Home).ShouldBe(@"~\scratch\notes");
    }

    [Fact]
    public void ShowAFolderOutsideHomeInFull()
    {
        FolderLabel.For(FolderPath.From(@"D:\work\Northwind.Tools"), [Repos, Worktrees], Home).ShouldBe(@"D:\work\Northwind.Tools");
    }

    [Fact]
    public void ShowARootItselfRelativeToHome()
    {
        FolderLabel.For(Repos, [Repos, Worktrees], Home).ShouldBe(@"~\source\repos");
    }

    [Fact]
    public void ShowTheHomeFolderAsATilde()
    {
        FolderLabel.For(Home, [Repos], Home).ShouldBe("~");
    }

    [Fact]
    public void NotMistakeASiblingThatSharesANamePrefixForAFolderUnderTheRoot()
    {
        FolderLabel.For(FolderPath.From(@"C:\Users\me\source\repos-old\Northwind.Orders"), [Repos], Home).ShouldBe(@"~\source\repos-old\Northwind.Orders");
    }
}