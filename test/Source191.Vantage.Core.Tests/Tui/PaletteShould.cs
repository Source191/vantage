using Source191.Vantage.Tui;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Core.Tests.Tui;

public sealed class PaletteShould
{
    [Fact]
    public void DrawFieldTextAtLeastFourAndAHalfToOneFromItsBackground()
    {
        ContrastRatio(Palette.White, Palette.FieldBackground).ShouldBeGreaterThanOrEqualTo(4.5);
    }

    [Fact]
    public void SetAFieldAtLeastThreeToOneApartFromTheConsolesBackground()
    {
        ContrastRatio(Palette.FieldBackground, Palette.Background).ShouldBeGreaterThanOrEqualTo(3.0);
    }

    /// <summary>The WCAG 2.2 contrast ratio between two colours, from their relative luminance.</summary>
    private static double ContrastRatio(Color first, Color second)
    {
        double lighter = Math.Max(Luminance(first), Luminance(second));
        double darker = Math.Min(Luminance(first), Luminance(second));

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Color colour) => (0.2126 * Linear(colour.R)) + (0.7152 * Linear(colour.G)) + (0.0722 * Linear(colour.B));

    private static double Linear(byte channel)
    {
        double value = channel / 255.0;

        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}