using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>The update tab was opened for this release; Vantage quits next.</summary>
public sealed record UpdateStartedOutcome(string Newest) : Outcome;
