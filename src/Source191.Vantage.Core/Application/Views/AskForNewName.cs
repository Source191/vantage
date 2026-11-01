using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The TUI asks for a new name, then raises <see cref="RenameConfirmed"/> with what was typed, or <see cref="RenameCancelled"/>.</summary>
public sealed record AskForNewName(BackgroundSession Session) : ViewEffect, IDialogEffect;