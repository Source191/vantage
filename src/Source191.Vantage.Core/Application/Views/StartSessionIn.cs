namespace Source191.Vantage.Application.Views;

/// <summary>The dialog effect behind <c>n</c> on a worktree row: the wizard fixed on that folder (<see cref="WizardOrigin.In"/>).</summary>
public sealed record StartSessionIn(WorktreeRow Row) : ViewEffect, IDialogEffect;