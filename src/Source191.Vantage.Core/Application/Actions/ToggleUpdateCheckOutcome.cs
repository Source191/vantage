using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>The update check is now on or off, and saved.</summary>
public sealed record ToggleUpdateCheckOutcome(bool On) : Outcome;