using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class NumberAwareOrderShould
{
    [Theory]
    [InlineData("2025.9", "2025.10")]
    [InlineData("253.99", "253.100")]
    [InlineData("a9", "a10")]
    [InlineData("a10", "b1")]
    [InlineData("abc2", "ABC10")]
    [InlineData("7", "007")]
    [InlineData("a007b", "a7c")]
    [InlineData(@"C:\Program Files\JetBrains\JetBrains Rider 2025.9\bin\rider64.exe", @"C:\Program Files\JetBrains\JetBrains Rider 2025.10\bin\rider64.exe")]
    [InlineData(@"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\253.99\bin\rider64.exe", @"C:\Users\me\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\253.100\bin\rider64.exe")]
    public void PutTheEarlierBeforeTheLater(string earlier, string later)
    {
        NumberAwareOrder.Compare(earlier, later).ShouldBeLessThan(0);
        NumberAwareOrder.Compare(later, earlier).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TieOnARunOfDigitsWithLeadingZeros()
    {
        NumberAwareOrder.Compare("a007b", "a7b").ShouldBeGreaterThan(0);
        NumberAwareOrder.Compare("a007c", "a7b").ShouldBeGreaterThan(0);
        NumberAwareOrder.Compare("a007a", "a7b").ShouldBeLessThan(0);
    }

    [Fact]
    public void AnswerZeroForTheSameText() => NumberAwareOrder.Compare("Rider 2025.10", "Rider 2025.10").ShouldBe(0);

    [Fact]
    public void SortReleasesByNumber()
    {
        string[] releases = ["Rider 2025.10", "Rider 2025.9", "Rider 2025.2"];

        string[] sorted = [.. releases.Order(Comparer<string>.Create(NumberAwareOrder.Compare))];

        sorted.ShouldBe(["Rider 2025.2", "Rider 2025.9", "Rider 2025.10"]);
    }
}