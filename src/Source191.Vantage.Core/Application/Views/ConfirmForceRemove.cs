namespace Source191.Vantage.Application.Views;

/// <summary>The TUI asks the user to type the confirmation text, then raises RemoveWorktree with Forced.</summary>
public sealed record ConfirmForceRemove(WorktreeRow Row, string ConfirmationText) : ViewEffect, IDialogEffect;