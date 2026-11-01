namespace Source191.Vantage.Application.Diagnostics;

public sealed record TimedEvent(DateTimeOffset At, DiagnosticEvent Event);