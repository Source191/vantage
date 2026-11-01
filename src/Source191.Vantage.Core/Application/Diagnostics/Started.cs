using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Diagnostics;

public sealed record Started(ToolVersion Vantage, Version? ClaudeCode, Version Windows, Version Runtime, int Columns, int Rows) : DiagnosticEvent;