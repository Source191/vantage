namespace Source191.Vantage.Infrastructure.Tests.Contract;

/// <summary>
/// The commands and options a help text offers, without their descriptions, so a snapshot changes only when the
/// CLI's surface does. An entry line has exactly two leading spaces; its descriptions' continuation lines have more.
/// The entry's names end at the first double space: "-n, --name &lt;name&gt;" gives -n and --name, "stop|kill &lt;id&gt;"
/// gives stop and kill.
/// </summary>
public static class HelpSurface
{
    private const string EntryIndent = "  ";

    private static readonly string[] Sections = ["Options:", "Commands:"];

    public static IReadOnlyList<string> Extract(string help)
    {
        return [.. Entries(help.Split('\n'))
            .SelectMany(Names)
            .Distinct()
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>The entry lines under an Options or Commands heading; Arguments and prose are skipped.</summary>
    private static IEnumerable<string> Entries(IEnumerable<string> lines)
    {
        bool inSection = false;
        foreach (string line in lines)
        {
            inSection = IsHeading(line) ? Sections.Contains(line.Trim()) : inSection;
            if (inSection && IsEntry(line))
            {
                yield return line;
            }
        }
    }

    private static bool IsHeading(string line)
    {
        return line.Length > 0 && line[0] != ' ' && line.TrimEnd().EndsWith(':');
    }

    private static bool IsEntry(string line)
    {
        return line.StartsWith(EntryIndent, StringComparison.Ordinal) && line.Length > EntryIndent.Length && line[EntryIndent.Length] != ' ';
    }

    private static IEnumerable<string> Names(string line)
    {
        string column = line.Trim().Split(EntryIndent)[0];

        return column.StartsWith('-')
            ? column.Split(", ").Select(FirstWord)
            : FirstWord(column).Split('|');
    }

    private static string FirstWord(string text)
    {
        return text.Split(' ')[0];
    }
}