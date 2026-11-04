using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>The update tab was opened for this release; Torni quits next.</summary>
public sealed record UpdateStartedOutcome(string Newest) : Outcome;