using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>The words around the table screens: the tab title, the header's values, the table title, the key hints and the help.</summary>
public static partial class Wording
{
    private const string UnknownVersion = "unknown";

    /// <summary>The title of Vantage's own tab; every writer of it uses this one value.</summary>
    public static string TabTitle(ToolVersion version) => $"Vantage ({version.Text})";

    /// <summary>The header's first three values, one per line: Vantage's version, the Claude Code version and the repository roots.</summary>
    public static string HeaderValues(ToolVersion vantage, string? claudeVersion, IReadOnlyList<string> roots)
    => string.Join('\n', vantage.Text, claudeVersion ?? UnknownVersion, string.Join("; ", roots));
}