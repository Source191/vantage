namespace Source191.Torni.Domain;

/// <summary>
/// The ordered tab colours, arranged so that the first six (Torni's own green and the first five session tabs)
/// stay apart for protanopia and deuteranopia as well as for normal colour vision.
/// </summary>
public static class TabPalette
{
    public static TabColour Red { get; } = new("Red", "#E74856");

    public static TabColour Blue { get; } = new("Blue", "#0078D4");

    public static TabColour Green { get; } = new("Green", "#16C60C");

    public static TabColour Teal { get; } = new("Teal", "#00B7C3");

    public static TabColour Pink { get; } = new("Pink", "#E3008C");

    public static TabColour Brown { get; } = new("Brown", "#8E562E");

    public static TabColour Purple { get; } = new("Purple", "#8764B8");

    public static TabColour Orange { get; } = new("Orange", "#F7630C");

    public static TabColour Lime { get; } = new("Lime", "#9ACD32");

    public static TabColour Grey { get; } = new("Grey", "#7A7574");

    public static TabColour Yellow { get; } = new("Yellow", "#FFB900");

    public static IReadOnlyList<TabColour> Default { get; } = [Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow];

    /// <summary>The colour furthest down the order, the one a session tab is least likely to be given.</summary>
    public static TabColour Last => Default[^1];
}