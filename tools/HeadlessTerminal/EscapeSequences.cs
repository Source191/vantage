using System.Text.RegularExpressions;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Strips <see href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html">CSI, OSC and DCS sequences</see> from a
/// capture so plain text can be searched.
/// </summary>
public static partial class EscapeSequences
{
    public static string Strip(string raw) => OscOrDcs().Replace(Csi().Replace(raw, string.Empty), string.Empty);

    [GeneratedRegex(@"\x1b\[[0-9;?<>=]*[A-Za-z_@`]")]
    private static partial Regex Csi();

    [GeneratedRegex(@"\x1b[\]P][^\x07\x1b]*(\x07|\x1b\\)")]
    private static partial Regex OscOrDcs();
}