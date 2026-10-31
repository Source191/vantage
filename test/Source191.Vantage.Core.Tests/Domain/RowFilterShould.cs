using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class RowFilterShould
{
    [Fact]
    public void MatchWhenAnyFieldContainsTheTextIgnoringCase()
    {
        RowFilter filter = new("TEMPLATES");

        filter.Matches(["remove-sonar", "Northwind.Templates", "main"]).ShouldBeTrue();
    }

    [Fact]
    public void NotMatchWhenNoFieldContainsTheText()
    {
        RowFilter filter = new("catalogue");

        filter.Matches(["remove-sonar", "Northwind.Templates", "main"]).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchEverythingWhenBlank(string text)
    {
        RowFilter filter = new(text);

        filter.Matches(["anything"]).ShouldBeTrue();
    }

    [Fact]
    public void MatchAmongTheFieldsARowHasAndNotWhenItHasNone()
    {
        RowFilter filter = new("main");

        filter.Matches(["remove-sonar", "main"]).ShouldBeTrue();
        filter.Matches([]).ShouldBeFalse();
    }
}