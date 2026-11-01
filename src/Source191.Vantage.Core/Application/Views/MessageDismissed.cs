namespace Source191.Vantage.Application.Views;

/// <summary>The status line's close mark was clicked, or a success's time ran out: clears that message, and never a newer one.</summary>
public sealed record MessageDismissed(StatusMessage Message) : ViewInput;