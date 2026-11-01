using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>The session was stopped; it stays listed and can be resumed. TabClosed when a Vantage tab showing it was closed too.</summary>
public sealed record StopSessionOutcome(string Name, bool TabClosed) : Outcome;