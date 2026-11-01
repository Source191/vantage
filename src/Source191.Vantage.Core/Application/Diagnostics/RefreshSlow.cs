namespace Source191.Vantage.Application.Diagnostics;

public sealed record RefreshSlow(RefreshPart Part, TimeSpan Duration) : DiagnosticEvent;
