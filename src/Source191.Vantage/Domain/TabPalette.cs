namespace Source191.Vantage.Domain;

public sealed record TabColour(string Name, string Hex);

/// <summary>
/// The ordered tab colours, arranged so that neighbouring colours contrast well.
/// </summary>
public static class TabPalette
{
    public static IReadOnlyList<TabColour> Default { get; } =
    [
        new("Red", "#E74856"),
        new("Blue", "#0078D4"),
        new("Yellow", "#FFB900"),
        new("Purple", "#8764B8"),
        new("Green", "#16C60C"),
        new("Orange", "#F7630C"),
        new("Teal", "#00B7C3"),
        new("Pink", "#E3008C"),
        new("Brown", "#8E562E"),
        new("Lime", "#9ACD32"),
        new("Grey", "#7A7574"),
    ];
}