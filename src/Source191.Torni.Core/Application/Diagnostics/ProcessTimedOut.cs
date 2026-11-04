namespace Source191.Torni.Application.Diagnostics;

public sealed record ProcessTimedOut(ChildCommand Command, TimeSpan Limit) : DiagnosticEvent;