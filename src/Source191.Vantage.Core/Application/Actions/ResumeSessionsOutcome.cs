using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>How many of the chosen sessions resumed, and a note for each that did not.</summary>
public sealed record ResumeSessionsOutcome(int Resumed, int Chosen, IReadOnlyList<ResumeNote> Notes) : Outcome;