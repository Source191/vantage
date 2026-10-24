using System.Text;

namespace Source191.Vantage.Application;

/// <summary>
/// Builds the PowerShell that runs inside a tab Vantage opens. Every value is single-quoted, so user text such as a
/// session name is never interpolated into script source; the adapter encodes the finished script for wt.exe.
/// </summary>
public static class PowerShellScript
{
    /// <summary>
    /// Every character PowerShell's tokenizer reads as a single quote: the ASCII apostrophe and U+2018 to U+201B.
    /// Any of them would end the literal, so each is doubled with itself and the text comes back unchanged.
    /// </summary>
    private static readonly char[] SingleQuotes = ['\'', '‘', '’', '‚', '‛'];

    /// <summary>The only way a value enters a tab's script: a single-quoted literal PowerShell reads back unchanged.</summary>
    public static string Quote(string text)
    {
        StringBuilder quoted = new(text.Length + 2);
        quoted.Append('\'');
        foreach (char character in text)
        {
            quoted.Append(character);
            if (Array.IndexOf(SingleQuotes, character) >= 0)
            {
                quoted.Append(character);
            }
        }

        return quoted.Append('\'').ToString();
    }

    public static string Invoke(string executable, IReadOnlyList<string> arguments)
    {
        IEnumerable<string> quoted = new[] { executable }.Concat(arguments).Select(Quote);

        return $"& {string.Join(' ', quoted)}";
    }
}