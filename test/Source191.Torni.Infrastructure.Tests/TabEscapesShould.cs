using Source191.Torni.Adapters.Terminal;
using Source191.Torni.Domain;

namespace Source191.Torni.Infrastructure.Tests;

public sealed class TabEscapesShould
{
    [Fact]
    public void SetTheTabTitleWithOsc2()
    {
        TabEscapes.Title("Torni").ShouldBe("\u001b]2;Torni\u0007");
        TabEscapes.ClearTitle.ShouldBe("\u001b]2;\u0007");
    }

    [Fact]
    public void SetTheTabBackgroundThroughPaletteEntry264()
    {
        TabEscapes.Background(new TabColour("Green", "#16C60C")).ShouldBe("\u001b]4;264;rgb:16/c6/0c\u001b\\");
    }

    [Fact]
    public void ResetPaletteEntry264ThenTheTabColourIndices()
    {
        TabEscapes.ResetBackground.ShouldBe("\u001b]104;264\u001b\\" + "\u001b[2;263;264,|");
    }
}