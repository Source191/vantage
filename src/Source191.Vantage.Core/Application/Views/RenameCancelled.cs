using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The new name dialog was closed without an answer.</summary>
public sealed record RenameCancelled(BackgroundSession Session) : ViewInput;