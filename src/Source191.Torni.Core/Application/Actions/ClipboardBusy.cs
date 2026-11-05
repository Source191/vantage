using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>Another program held the clipboard, so nothing was copied.</summary>
public sealed record ClipboardBusy : Outcome;