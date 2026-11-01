using System.Text.RegularExpressions;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Answers terminal queries the way Windows Terminal 1.25 would:
/// <see href="https://sw.kovidgoyal.net/kitty/keyboard-protocol/">kitty keyboard flags</see>,
/// <see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences">primary</see> and
/// <see href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html">secondary device attributes, XTVERSION</see>,
/// cursor position and background colour. The XTVERSION reply can be delayed so it lands
/// after the program has exited, which is what happens to Vantage after a detach.
/// </summary>
public sealed class TerminalQueries(int xtversionDelayMs)
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly (Regex Query, string Reply)[] Answers =
    [
        (Query(@"\x1b\[>0?q"), "\x1bP>|Windows Terminal 1.25.2733.0\x1b\\"),
        (Query(@"\x1b\[\?u"), "\x1b[?0u"),
        (Query(@"\x1b\[0?c"), "\x1b[?61;4;6;7;14;21;22;23;24;28;32;42c"),
        (Query(@"\x1b\[>0?c"), "\x1b[>0;10;1c"),
        (Query(@"\x1b\[6n"), "\x1b[1;1R"),
        (Query(@"\x1b\]11;\?(\x07|\x1b\\)"), "\x1b]11;rgb:0c0c/0c0c/0c0c\x1b\\"),
    ];

    private static Regex Query(string pattern) => new(pattern, RegexOptions.None, MatchTimeout);

    public IReadOnlyList<TerminalReply> RepliesTo(string output)
    {
        List<TerminalReply> replies = [];
        foreach ((Regex query, string reply) in Answers)
        {
            int delay = reply.StartsWith("\x1bP", StringComparison.Ordinal) ? xtversionDelayMs : 0;
            for (int i = 0; i < query.Count(output); i++)
            {
                replies.Add(new TerminalReply(reply, delay));
            }
        }

        return replies;
    }
}