using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>
/// A background session's screen rebuilt from <c>claude logs</c>, which answers the whole screen history as one stream:
/// every row padded with spaces to the session's own width, with no line breaks, and the width not stated. Claude draws
/// its input box between two rules as wide as the screen, so the last rule gives the width.
/// </summary>
public static partial class SessionScreen
{
    private const char Rule = '─';

    /// <summary>The last <paramref name="rows"/> rows of the session's output, without Claude's own input box and status line; none when no rule gives the width.</summary>
    public static IReadOnlyList<string> Bottom(string logs, int rows)
    {
        ArgumentNullException.ThrowIfNull(logs);
        string plain = AnsiText.Strip(logs);
        if (WidthOf(plain) is not int width)
        {
            return [];
        }

        List<string> lines = [.. Enumerable.Range(0, plain.Length / width).Select(row => plain.Substring(row * width, width).TrimEnd())];

        return [.. WithoutTrailingBlanks(WithoutInputBox(lines)).TakeLast(rows)];
    }

    /// <summary>The length of the last rule, when it starts a row of that width and the text divides into such rows.</summary>
    private static int? WidthOf(string plain)
    {
        Match? last = RuleRun().Matches(plain).LastOrDefault();
        int width = last?.Length ?? 0;

        return width > 0 && plain.Length % width == 0 && last!.Index % width == 0 ? width : null;
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

    [GeneratedRegex("─{20,}")]
    private static partial Regex RuleRun();
}