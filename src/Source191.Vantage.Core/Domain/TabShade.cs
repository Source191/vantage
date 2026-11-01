using System.Globalization;

namespace Source191.Vantage.Domain;

/// <summary>
/// The colour Windows Terminal shows for a coloured tab while another tab is selected: the tab's colour at alpha 77
/// (its own 30 %, set in
/// <see href="https://github.com/microsoft/terminal/blob/v1.25.2733.0/src/cascadia/TerminalApp/Tab.cpp#L2387">Tab.cpp</see>)
/// over the dark theme's tab row, measured on Windows Terminal 1.25.
/// </summary>
public static class TabShade
{
    private const int Alpha = 77;
    private const int TabRow = 0x2E;

    /// <summary>The shade as <c>#RRGGBB</c>; a colour that is not <c>#</c> and six hex digits (a hand-edited state file) is answered as it is.</summary>
    public static string Unselected(TabColour colour)
    {
        ArgumentNullException.ThrowIfNull(colour);
        if (colour.Hex is not ['#', _, _, _, _, _, _]
            || !int.TryParse(colour.Hex.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value))
        {
            return colour.Hex;
        }

        return $"#{Blend(value >> 16):X2}{Blend(value >> 8):X2}{Blend(value):X2}";
    }

    private static int Blend(int channel) => (((channel & 0xFF) * Alpha) + (TabRow * (255 - Alpha))) / 255;
}