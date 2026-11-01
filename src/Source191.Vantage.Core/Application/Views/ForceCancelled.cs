namespace Source191.Vantage.Application.Views;

/// <summary>The force-remove confirmation was closed without an answer.</summary>
public sealed record ForceCancelled(WorktreeRow Row) : ViewInput;