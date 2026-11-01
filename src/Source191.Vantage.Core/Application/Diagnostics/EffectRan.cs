namespace Source191.Vantage.Application.Diagnostics;

public sealed record EffectRan(CodeName Effect, CodeName Outcome, TimeSpan Duration) : DiagnosticEvent;