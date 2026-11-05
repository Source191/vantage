using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>The update check is now on or off, and saved.</summary>
public sealed record ToggleUpdateCheckOutcome(bool On) : Outcome;