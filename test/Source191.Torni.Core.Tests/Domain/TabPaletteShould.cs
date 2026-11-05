using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class TabPaletteShould
{
    [Fact]
    public void OrderTheColoursSoTheFirstSixStayApartForRedGreenColourBlindness()
    {
        TabPalette.Default.Select(colour => colour.Name).ShouldBe(
            ["Red", "Blue", "Green", "Teal", "Pink", "Brown", "Purple", "Orange", "Lime", "Grey", "Yellow"]);
    }

    [Fact]
    public void GiveEveryColourTheWindowsTerminalHexItIsNamedFor()
    {
        TabPalette.Default.Select(colour => $"{colour.Name} {colour.Hex}").ShouldBe(
            ["Red #E74856", "Blue #0078D4", "Green #16C60C", "Teal #00B7C3", "Pink #E3008C", "Brown #8E562E", "Purple #8764B8", "Orange #F7630C", "Lime #9ACD32", "Grey #7A7574", "Yellow #FFB900"]);
    }

    [Fact]
    public void NameEachColourAndBuildTheOrderFromThem()
    {
        TabPalette.Green.Name.ShouldBe("Green");
        TabPalette.Yellow.Name.ShouldBe("Yellow");
        TabPalette.Last.ShouldBe(TabPalette.Default[^1]);
        TabPalette.Default.Count(colour => colour == TabPalette.Green).ShouldBe(1);
        TabPalette.Default.Count(colour => colour == TabPalette.Yellow).ShouldBe(1);
    }
}