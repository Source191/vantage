using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class TextFitShould
{
    [Fact]
    public void KeepTheOddCharacterAtTheHead()
    {
        TextFit.ElideMiddle("Northwind.Orders", 10).ShouldBe("North…ders");
    }

    [Fact]
    public void ReturnTextThatFits()
    {
        TextFit.ElideMiddle("Northwind", 9).ShouldBe("Northwind");
        TextFit.ElideMiddle("Northwind", 20).ShouldBe("Northwind");
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "N")]
    [InlineData(2, "No")]
    public void CutWithoutAnEllipsisBelowThree(int width, string expected)
    {
        TextFit.ElideMiddle("Northwind.Orders", width).ShouldBe(expected);
    }

    [Fact]
    public void ShowACharacterEachSideAtWidthThree()
    {
        TextFit.ElideMiddle("Northwind.Orders", 3).ShouldBe("N…s");
    }

    [Fact]
    public void RefuseANegativeWidth()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TextFit.ElideMiddle("Northwind", -1));
    }
}