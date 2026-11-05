namespace Source191.Torni.Application.Diagnostics;

public sealed record RefreshSlow(RefreshPart Part, TimeSpan Duration) : DiagnosticEvent;