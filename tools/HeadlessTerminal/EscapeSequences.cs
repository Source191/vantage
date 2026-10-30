using Source191.Vantage.Domain;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>Strips CSI, OSC and DCS sequences from a capture so plain text can be searched; the rule is Core's, which the detail pane uses too.</summary>
public static class EscapeSequences
{
    public static string Strip(string raw) => AnsiText.Strip(raw);
}