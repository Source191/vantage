using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>Another program held the clipboard, so nothing was copied.</summary>
public sealed record ClipboardBusy : Outcome;