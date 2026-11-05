namespace Source191.Torni.Domain;

/// <summary>Fits text into a cell's width. Widths are UTF-16 characters, as everywhere in the console's cells.</summary>
public static class TextFit
{
    public const string Ellipsis = "…";

    /// <summary>A character, the ellipsis and a character: the least an elision can show.</summary>
    private const int LeastElided = 3;

    /// <summary>
    /// The text within <paramref name="width"/> characters: whole when it fits, else its start and its end around an ellipsis
    /// (the odd character kept at the start), and below three characters cut at its end with no ellipsis.
    /// </summary>
    public static string ElideMiddle(string text, int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);

        if (text.Length <= width)
        {
            return text;
        }

        if (width < LeastElided)
        {
            return text[..width];
        }

        int kept = width - Ellipsis.Length;
        int head = (kept + 1) / 2;

        return text[..head] + Ellipsis + text[^(kept - head)..];
    }
}