using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>The first start of a new version found no release notes it could read inside the installed tool, so What's new was not shown.</summary>
public sealed record PackagedNotesUnreadable(ToolVersion Running) : DiagnosticEvent;