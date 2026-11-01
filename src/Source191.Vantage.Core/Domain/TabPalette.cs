namespace Source191.Vantage.Domain;

/// <summary>
/// The ordered tab colours, arranged so that the first six (Vantage's own green and the first five session tabs)
/// stay apart for protanopia and deuteranopia as well as for normal colour vision.
/// </summary>
public static class TabPalette
{
    public static IReadOnlyList<TabColour> Default { get; } =
    [
        new("Red", "#E74856"),
        new("Blue", "#0078D4"),
        new("Green", "#16C60C"),
        new("Teal", "#00B7C3"),
        new("Pink", "#E3008C"),
        new("Brown", "#8E562E"),
        new("Purple", "#8764B8"),
        new("Orange", "#F7630C"),
        new("Lime", "#9ACD32"),
        new("Grey", "#7A7574"),
        new("Yellow", "#FFB900"),
    ];
}