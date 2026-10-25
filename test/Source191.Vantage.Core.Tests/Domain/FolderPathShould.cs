using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class FolderPathShould
{
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";

    [Fact]
    public void EqualIgnoringCaseAndTrailingSeparators()
    {
        FolderPath typed = FolderPath.From(Repository);
        FolderPath other = FolderPath.From(@"c:\users\ME\source\repos\northwind.orders\");

        other.ShouldBe(typed);
        other.GetHashCode().ShouldBe(typed.GetHashCode());
        new HashSet<FolderPath> { typed }.ShouldContain(other);
        FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing").ShouldNotBe(typed);
    }

    [Fact]
    public void ConvertForwardSlashes()
    {
        FolderPath.From("C:/Users/me/source/repos/Northwind.Orders").Value.ShouldBe(Repository);
    }

    [Fact]
    public void KeepTheCaseItWasGiven()
    {
        FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders\").Value.ShouldBe(Repository);
    }

    [Fact]
    public void ExposeNameAndParent()
    {
        FolderPath path = FolderPath.From(Repository);

        path.Name.ShouldBe("Northwind.Orders");
        path.Parent.ShouldBe(FolderPath.From(@"C:\Users\me\source\repos"));
        path.Combine("Northwind.Orders-fix-123").Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Orders\Northwind.Orders-fix-123");
    }

    [Fact]
    public void KeepADriveRootWholeAndGiveItNoParent()
    {
        FolderPath root = FolderPath.From(@"C:\");

        root.Value.ShouldBe(@"C:\");
        root.Parent.ShouldBeNull();
    }

    [Fact]
    public void KnowWhenItIsUnderAnother()
    {
        FolderPath repository = FolderPath.From(Repository);

        FolderPath.From(Repository + @"\src\Orders").IsUnder(repository).ShouldBeTrue();
        repository.IsUnder(repository).ShouldBeTrue("a folder is under itself, as a session started in it is");
        FolderPath.From(Repository + "-fix-123").IsUnder(repository).ShouldBeFalse("a sibling that shares the name as a prefix is not inside it");
        repository.IsUnder(FolderPath.From(Repository + @"\src")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"repos\Northwind.Orders")]
    [InlineData(@"C:repos\Northwind.Orders")]
    public void RejectRelativePaths(string text)
    {
        Should.Throw<ArgumentException>(() => FolderPath.From(text));
    }

    [Fact]
    public void GiveTheSameKeyToPathsThatAreTheSameFolder()
    {
        FolderPath.From(@"c:\users\ME\source\repos\northwind.orders\").Key.ShouldBe(FolderPath.From(Repository).Key);
        FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing").Key.ShouldNotBe(FolderPath.From(Repository).Key);
    }

    [Theory]
    [InlineData(Repository, Repository)]
    [InlineData("C:/Users/me/source/repos/Northwind.Orders", Repository)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData(@"repos\Northwind.Orders", null)]
    public void ParseTextFromOutsideWithoutThrowing(string? text, string? expected)
    {
        FolderPath.TryParse(text, out FolderPath? path).ShouldBe(expected is not null);
        path?.Value.ShouldBe(expected);
        (path is null).ShouldBe(expected is null);
    }

    [Fact]
    public void ReadAsThePathItself()
    {
        $"{FolderPath.From(Repository)}".ShouldBe(Repository);
    }
}