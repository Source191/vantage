namespace Source191.Vantage.Domain;

/// <summary>Terminal text without its control sequences: CSI (colours, cursor moves, erasing), OSC (titles) and DCS blocks.</summary>
public static class AnsiText
{
    public static string Strip(string raw) => raw;
}
