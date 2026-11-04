using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Close the tabs of plain claude sessions that were never sent a message: nothing to keep.</summary>
public sealed record CloseEmptyTerminals(IReadOnlyList<Session.Interactive> Sessions) : ViewEffect, IDialogEffect;