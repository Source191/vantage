using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

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
}
