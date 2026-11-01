using Source191.Vantage.Domain;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// The escape sequences a program can use to label its own Windows Terminal tab:
/// <see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences">OSC 2</see> for the title,
/// <see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences">OSC 4</see> on palette entry
/// 264 (the tab background) for the colour, and
/// <see href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html">OSC 104</see> with
/// <see href="https://vt100.net/dec/ek-vt520-rm.pdf">DECAC</see> to put the entry and the tab's colour indices back.
/// Ignored by the terminal when the tab was launched with an explicit
/// <see href="https://learn.microsoft.com/windows/terminal/command-line-arguments">tabColor</see>.
/// </summary>
public static class TabEscapes
{
    public const string ClearTitle = "\u001b]2;\u0007";

    /// <summary>OSC 104 puts entry 264 back to the terminal's own colour; DECAC alone would only re-point the tab at an entry still holding Vantage's colour.</summary>
    public const string ResetBackground = "\u001b]104;264\u001b\\\u001b[2;263;264,|";

    public static string Title(string title) => $"\u001b]2;{title}\u0007";

    public static string Background(TabColour colour)
    {
        string hex = colour.Hex.TrimStart('#').ToLowerInvariant();

        return $"\u001b]4;264;rgb:{hex[..2]}/{hex[2..4]}/{hex[4..6]}\u001b\\";
    }
}