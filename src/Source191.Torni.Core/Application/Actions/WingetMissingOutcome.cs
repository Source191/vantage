using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>winget is not on the PATH, so no update tab was opened and the console stays; the release page has the zip.</summary>
public sealed record WingetMissingOutcome(string Newest) : Outcome;