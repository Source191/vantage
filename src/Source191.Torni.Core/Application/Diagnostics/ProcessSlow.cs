namespace Source191.Torni.Application.Diagnostics;

public sealed record ProcessSlow(ChildCommand Command, int ExitCode, TimeSpan Duration) : DiagnosticEvent;