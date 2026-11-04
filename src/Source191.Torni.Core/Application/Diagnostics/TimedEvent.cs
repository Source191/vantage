namespace Source191.Torni.Application.Diagnostics;

public sealed record TimedEvent(DateTimeOffset At, DiagnosticEvent Event);