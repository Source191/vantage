using Source191.Torni.Domain;

namespace Source191.Torni.Application.Diagnostics;

/// <summary>The first start of a new version found no release notes it could read inside the installed tool, so What's new was not shown.</summary>
public sealed record PackagedNotesUnreadable(ToolVersion Running) : DiagnosticEvent;