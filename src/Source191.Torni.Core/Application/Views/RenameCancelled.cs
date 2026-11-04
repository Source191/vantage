using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>The new name dialog was closed without an answer.</summary>
public sealed record RenameCancelled(Session.Background Session) : ViewInput;