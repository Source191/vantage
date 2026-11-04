namespace Source191.Torni.Application.Views;

/// <summary>What the user typed into the force-remove confirmation.</summary>
public sealed record ForceConfirmed(WorktreeRow Row, string ConfirmationText, string Typed) : ViewInput;