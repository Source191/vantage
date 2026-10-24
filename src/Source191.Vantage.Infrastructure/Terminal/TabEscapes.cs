using Source191.Vantage.Domain;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// The escape sequences a program can use to label its own Windows Terminal tab: OSC 2 for the title, OSC 4 on
/// palette entry 264 (the tab background) for the colour, and DECAC to put the tab's colour indices back.
/// Ignored by the terminal when the tab was launched with an explicit tabColor.
/// </summary>
public static class TabEscapes
{
    public const string ClearTitle = "\u001b]2;\u0007";

    public const string ResetBackground = "\u001b[2;263;264,|";

    public static string Title(string title) => $"\u001b]2;{title}\u0007";

    public static string Background(TabColour colour)
    {
        string hex = colour.Hex.TrimStart('#').ToLowerInvariant();

        return $"\u001b]4;264;rgb:{hex[..2]}/{hex[2..4]}/{hex[4..6]}\u001b\\";
    }
}