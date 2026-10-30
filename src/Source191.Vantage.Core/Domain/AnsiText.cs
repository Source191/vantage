using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>Terminal text without its control sequences: CSI (colours, cursor moves, erasing), OSC (titles) and DCS blocks.</summary>
public static partial class AnsiText
{
    public static string Strip(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        return OscOrDcs().Replace(Csi().Replace(raw, string.Empty), string.Empty);
    }

    [GeneratedRegex(@"\x1b\[[0-9;?<>=]*[A-Za-z_@`]")]
    private static partial Regex Csi();

    [GeneratedRegex(@"\x1b[\]P][^\x07\x1b]*(\x07|\x1b\\)")]
    private static partial Regex OscOrDcs();
}