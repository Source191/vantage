using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>The TUI asks for a new name, then raises <see cref="RenameConfirmed"/> with what was typed, or <see cref="RenameCancelled"/>.</summary>
public sealed record AskForNewName(Session.Background Session) : ViewEffect, IDialogEffect;