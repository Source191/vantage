using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class TabColourSelectionShould
{
    private readonly TabColourSelection selection = new(TabPalette.Default);

    [Fact]
    public void ChooseTheFirstPaletteColourWhenNoneAreInUse()
    {
        ColourChoice choice = selection.Choose(inUse: []);

        choice.Colour.Name.ShouldBe("Red");
        choice.Colour.Hex.ShouldBe("#E74856");
        choice.Reused.ShouldBeFalse();
    }

    [Fact]
    public void SkipColoursAlreadyInUse()
    {
        ColourChoice choice = selection.Choose(inUse: [TabPalette.Default[0], TabPalette.Default[2]]);

        choice.Colour.Name.ShouldBe("Blue");
        choice.Reused.ShouldBeFalse();
    }

    [Fact]
    public void ReuseTheLeastUsedColourWhenAllAreInUse()
    {
        List<TabColour> inUse = [.. TabPalette.Default, TabPalette.Default[0]];

        ColourChoice choice = selection.Choose(inUse);

        choice.Colour.Name.ShouldBe("Blue");
        choice.Reused.ShouldBeTrue();
    }

    [Fact]
    public void BreakTiesByPaletteOrderWhenReusing()
    {
        ColourChoice choice = selection.Choose(inUse: [.. TabPalette.Default]);

        choice.Colour.Name.ShouldBe("Red");
        choice.Reused.ShouldBeTrue();
    }

    [Fact]
    public void PreferAColourWhenItIsFree()
    {
        ColourChoice choice = selection.Choose(inUse: [TabPalette.Default[0]], preferred: TabPalette.Default.Single(colour => colour.Name == "Green"));

        choice.Colour.Name.ShouldBe("Green");
        choice.Reused.ShouldBeFalse();
    }

    [Fact]
    public void FallBackToTheFirstFreeColourWhenThePreferredOneIsInUse()
    {
        ColourChoice choice = selection.Choose(inUse: [TabPalette.Default.Single(colour => colour.Name == "Green")], preferred: TabPalette.Default.Single(colour => colour.Name == "Green"));

        choice.Colour.Name.ShouldBe("Red");
        choice.Reused.ShouldBeFalse();
    }
}