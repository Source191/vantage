using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class FolderPathShould
{
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";

    [Fact]
    public void EqualIgnoringCaseAndTrailingSeparators()
    {
        FolderPath typed = new(Repository);
        FolderPath other = new(@"c:\users\ME\source\repos\northwind.orders\");

        other.ShouldBe(typed);
        other.GetHashCode().ShouldBe(typed.GetHashCode());
        new HashSet<FolderPath> { typed }.ShouldContain(other);
        new FolderPath(@"C:\Users\me\source\repos\Northwind.Billing").ShouldNotBe(typed);
    }

    [Fact]
    public void ConvertForwardSlashes()
    {
        new FolderPath("C:/Users/me/source/repos/Northwind.Orders").Value.ShouldBe(Repository);
    }

    [Fact]
    public void KeepTheCaseItWasGiven()
    {
        new FolderPath(@"C:\Users\me\source\repos\Northwind.Orders\").Value.ShouldBe(Repository);
    }

    [Fact]
    public void ExposeNameAndParent()
    {
        FolderPath path = new(Repository);

        path.Name.ShouldBe("Northwind.Orders");
        path.Parent.ShouldBe(new FolderPath(@"C:\Users\me\source\repos"));
        path.Combine("Northwind.Orders-fix-123").Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders\Northwind.Orders-fix-123");
    }

    [Fact]
    public void KeepADriveRootWholeAndGiveItNoParent()
    {
        FolderPath root = new(@"C:\");

        root.Value.ShouldBe(@"C:\");
        root.Parent.ShouldBeNull();
    }

    [Fact]
    public void KnowWhenItIsUnderAnother()
    {
        FolderPath repository = new(Repository);

        new FolderPath(Repository + @"\src\Orders").IsUnder(repository).ShouldBeTrue();
        repository.IsUnder(repository).ShouldBeTrue("a folder is under itself, as a session started in it is");
        new FolderPath(Repository + "-fix-123").IsUnder(repository).ShouldBeFalse("a sibling that shares the name as a prefix is not inside it");
        repository.IsUnder(new FolderPath(Repository + @"\src")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"repos\Northwind.Orders")]
    [InlineData(@"C:repos\Northwind.Orders")]
    public void RejectRelativePaths(string text)
    {
        Should.Throw<ArgumentException>(() => new FolderPath(text));
    }

    [Fact]
    public void GiveTheSameKeyToPathsThatAreTheSameFolder()
    {
        new FolderPath(@"c:\users\ME\source\repos\northwind.orders\").Key.ShouldBe(new FolderPath(Repository).Key);
        new FolderPath(@"C:\Users\me\source\repos\Northwind.Billing").Key.ShouldNotBe(new FolderPath(Repository).Key);
    }

    [Theory]
    [InlineData(Repository, Repository)]
    [InlineData("C:/Users/me/source/repos/Northwind.Orders", Repository)]
    [InlineData("", null)]
    [InlineData(@"repos\Northwind.Orders", null)]
    public void ParseTextFromOutsideWithoutThrowing(string text, string? expected)
    {
        FolderPath.TryParse(text)?.Value.ShouldBe(expected);
        (FolderPath.TryParse(text) is null).ShouldBe(expected is null);
    }

    [Fact]
    public void ReadAsThePathItself()
    {
        $"{new FolderPath(Repository)}".ShouldBe(Repository);
    }
}