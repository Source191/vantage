using System.Text;
using System.Text.RegularExpressions;

namespace Source191.Torni.HeadlessTerminal;

/// <summary>
/// What a smoke step waits for in the raw capture: a terminal title the program set
/// (<see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences">OSC 0 or OSC 2</see>, matched whole),
/// text it drew (matched with the escape sequences between its characters stripped), or nothing.
/// </summary>
public abstract partial record CaptureMarker
{
    public static readonly CaptureMarker Nothing = new NothingAtAll();

    /// <summary>The position in the raw capture just after the first match at or after <paramref name="from"/>, or null when there is none yet.</summary>
    public abstract int? FindAfter(string capture, int from);

    /// <summary>A title the program set, matched only when it is the whole title.</summary>
    public sealed record Title(string Text) : CaptureMarker
    {
        public override int? FindAfter(string capture, int from)
        {
            for (Match match = TitleSequence().Match(capture, from); match.Success; match = match.NextMatch())
            {
                if (match.Groups["title"].Value == Text)
                {
                    return match.Index + match.Length;
                }
            }

            return null;
        }

        public override string ToString() => $"the title \"{Text}\"";
    }

    /// <summary>Text the program drew; escape sequences between its characters are skipped, and text inside a title never matches.</summary>
    public sealed record ScreenText(string Text) : CaptureMarker
    {
        public override int? FindAfter(string capture, int from)
        {
            StringBuilder plain = new();
            List<int> rawIndex = [];
            int index = from;
            while (index < capture.Length)
            {
                if (capture[index] == '\x1b')
                {
                    index = EscapeSequences.EndOf(capture, index);
                    continue;
                }

                plain.Append(capture[index]);
                rawIndex.Add(index);
                index++;
            }

            int found = plain.ToString().IndexOf(Text, StringComparison.Ordinal);

            return found < 0 ? null : rawIndex[found + Text.Length - 1] + 1;
        }

        public override string ToString() => $"the text \"{Text}\"";
    }

    private sealed record NothingAtAll : CaptureMarker
    {
        public override int? FindAfter(string capture, int from) => from;

        public override string ToString() => "nothing";
    }

    [GeneratedRegex(@"\x1b\][02];(?<title>[^\x07\x1b]*)(\x07|\x1b\\)")]
    private static partial Regex TitleSequence();
}