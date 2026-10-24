using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class TabEscapesShould
{
    [Fact]
    public void SetTheTabTitleWithOsc2()
    {
        TabEscapes.Title("Vantage").ShouldBe("\u001b]2;Vantage\u0007");
        TabEscapes.ClearTitle.ShouldBe("\u001b]2;\u0007");
    }

    [Fact]
    public void SetTheTabBackgroundThroughPaletteEntry264()
    {
        TabEscapes.Background(new TabColour("Green", "#16C60C")).ShouldBe("\u001b]4;264;rgb:16/c6/0c\u001b\\");
    }

    [Fact]
    public void ResetTheTabColourIndicesWithDecac()
    {
        TabEscapes.ResetBackground.ShouldBe("\u001b[2;263;264,|");
    }
}