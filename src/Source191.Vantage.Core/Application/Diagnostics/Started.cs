using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>The start facts: versions and the terminal's size; <c>ClaudeCode</c> is null when <c>claude --version</c> could not be read.</summary>
public sealed record Started(ToolVersion Vantage, Version? ClaudeCode, Version Windows, Version Runtime, int Columns, int Rows) : DiagnosticEvent
{
    /// <summary>The start facts from Claude Code's version report, which carries a version only when its printed text is one.</summary>
    public static Started From(ToolVersion vantage, ClaudeVersionReport claude, Version windows, Version runtime, int columns, int rows)
    {
        Version? claudeCode = claude is ClaudeVersionReport.Printed printed && ClaudeVersion.TryParse(printed.Text, out Version? parsed) ? parsed : null;

        return new Started(vantage, claudeCode, windows, runtime, columns, rows);
    }
}