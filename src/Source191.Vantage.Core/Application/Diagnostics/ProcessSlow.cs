namespace Source191.Vantage.Application.Diagnostics;

public sealed record ProcessSlow(ChildCommand Command, int ExitCode, TimeSpan Duration) : DiagnosticEvent;