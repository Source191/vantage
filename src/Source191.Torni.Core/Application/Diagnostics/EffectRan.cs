namespace Source191.Torni.Application.Diagnostics;

public sealed record EffectRan(CodeName Effect, CodeName Outcome, TimeSpan Duration) : DiagnosticEvent;