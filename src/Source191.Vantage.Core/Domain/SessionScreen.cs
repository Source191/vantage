using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>
/// A background session's screen rebuilt from <c>claude logs</c>, which answers the whole screen history as one stream:
/// either every row padded with spaces to the session's own width with no line breaks (a session that has run for a
/// while) or the screen drawn line by line (a new one), and the width never stated. Claude draws its input box between
/// two rules as wide as the screen, so the longest rule gives the width, and the stream is replayed onto a grid that wide.
/// </summary>
public static partial class SessionScreen
{
    private const char Rule = '─';
    private const int ScreenHeight = 100;
    private const int MostRows = 20_000;

    /// <summary>The last <paramref name="rows"/> rows of the session's output, without Claude's own input box and status line; none when no rule gives the width.</summary>
    public static IReadOnlyList<string> Bottom(string logs, int rows)
    {
        ArgumentNullException.ThrowIfNull(logs);
        string plain = AnsiText.Strip(logs);
        if (WidthOf(plain) is not int width)
        {
            return [];
        }

        // Tall enough that nothing scrolls: every row the text could fill, every line break, and the screen's own height.
        int height = Math.Min((plain.Length / width) + plain.Count(character => character == '\n') + ScreenHeight, MostRows);
        List<string> lines = [.. TerminalReplay.Play(logs, width, height)];

        return [.. OneBlankAtATime(WithoutTrailingBlanks(WithoutInputBox(lines))).TakeLast(rows)];
    }

    /// <summary>The longest rule: Claude's input box is ruled across the whole screen, and nothing is wider than the screen.</summary>
    private static int? WidthOf(string plain)
    {
        int width = RuleRun().Matches(plain).Select(rule => rule.Length).DefaultIfEmpty(0).Max();

        return width > 0 ? width : null;
    }

    /// <summary>Claude draws its input box between two rules (the top one ends with the session's name) and its status line under them; everything from the second-last rule goes.</summary>
    private static List<string> WithoutInputBox(List<string> lines)
    {
        int[] rules = [.. lines.Select((line, index) => (line, index)).Where(row => RuleRun().IsMatch(row.line) && row.line.StartsWith(Rule)).Select(row => row.index)];

        return rules.Length >= 2 ? lines[..rules[^2]] : lines;
    }

    private static List<string> WithoutTrailingBlanks(List<string> lines)
    {
        int end = lines.FindLastIndex(line => line.Length > 0) + 1;

        return lines[..end];
    }

    /// <summary>A screen with little on it is mostly empty rows; a pane of a few rows keeps one between paragraphs.</summary>
    private static IEnumerable<string> OneBlankAtATime(List<string> lines) => lines.Where((line, index) => line.Length > 0 || index == 0 || lines[index - 1].Length > 0);

    [GeneratedRegex("─{20,}")]
    private static partial Regex RuleRun();
}