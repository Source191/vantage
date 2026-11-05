using System.Text;

namespace Source191.Torni.HeadlessTerminal;

/// <summary>
/// Finds the end of an <see href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html">ECMA-48 escape sequence</see>
/// (CSI, OSC or DCS, or a two-character escape) in a capture, and strips them so plain text can be searched.
/// </summary>
public static class EscapeSequences
{
    private const char Escape = '\x1b';
    private const char Bell = '\x07';

    public static string Strip(string raw)
    {
        StringBuilder plain = new();
        int index = 0;
        while (index < raw.Length)
        {
            if (raw[index] == Escape)
            {
                index = EndOf(raw, index);
                continue;
            }

            plain.Append(raw[index]);
            index++;
        }

        return plain.ToString();
    }

    /// <summary>
    /// The index after the sequence that starts with the escape at <paramref name="start"/>: for CSI, parameter bytes
    /// (0x30 to 0x3F), intermediate bytes (0x20 to 0x2F) and a final byte (0x40 to 0x7E), stopping early at any other
    /// byte; for OSC and DCS, up to BEL or ESC and a backslash; otherwise the escape and the character after it. A
    /// sequence the capture ends in the middle of runs to its end.
    /// </summary>
    public static int EndOf(ReadOnlySpan<char> text, int start)
    {
        if (start + 1 >= text.Length)
        {
            return text.Length;
        }

        return text[start + 1] switch
        {
            '[' => EndOfControlSequence(text, start + 2),
            ']' or 'P' => EndOfString(text, start + 2),
            _ => start + 2,
        };
    }

    private static int EndOfControlSequence(ReadOnlySpan<char> text, int from)
    {
        int index = from;
        while (index < text.Length && text[index] is >= '\x30' and <= '\x3F')
        {
            index++;
        }

        while (index < text.Length && text[index] is >= '\x20' and <= '\x2F')
        {
            index++;
        }

        if (index >= text.Length)
        {
            return text.Length;
        }

        return text[index] is >= '\x40' and <= '\x7E' ? index + 1 : index;
    }

    private static int EndOfString(ReadOnlySpan<char> text, int from)
    {
        for (int index = from; index < text.Length; index++)
        {
            if (text[index] == Bell)
            {
                return index + 1;
            }

            if (text[index] == Escape && index + 1 < text.Length && text[index + 1] == '\\')
            {
                return index + 2;
            }
        }

        return text.Length;
    }
}