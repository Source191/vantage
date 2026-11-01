using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Close the tabs of plain claude sessions that were never sent a message: nothing to keep.</summary>
public sealed record CloseEmptyTerminals(IReadOnlyList<InteractiveSession> Sessions) : ViewEffect, IDialogEffect;