namespace Source191.Torni.Application.Diagnostics;

public sealed record UiThreadStalled(TimeSpan Lateness) : DiagnosticEvent;