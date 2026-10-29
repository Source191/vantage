using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>Turns the Claude Code version text the header shows into a number, so a diagnostic record never holds the text itself.</summary>
public static class ClaudeVersion
{
    public static bool TryParse(string? text, [NotNullWhen(true)] out Version? version) => Version.TryParse(text, out version);
}