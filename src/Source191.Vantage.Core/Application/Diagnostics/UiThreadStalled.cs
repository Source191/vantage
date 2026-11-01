namespace Source191.Vantage.Application.Diagnostics;

public sealed record UiThreadStalled(TimeSpan Lateness) : DiagnosticEvent;