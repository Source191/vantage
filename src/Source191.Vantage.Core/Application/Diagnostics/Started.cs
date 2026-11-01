using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>The start facts: versions and the terminal's size; <c>ClaudeCode</c> is null when <c>claude --version</c> could not be read.</summary>
public sealed record Started(ToolVersion Vantage, Version? ClaudeCode, Version Windows, Version Runtime, int Columns, int Rows) : DiagnosticEvent;