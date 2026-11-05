using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Open the job in its own tab, or switch to the tab that already shows it.</summary>
public sealed record OpenTabForSession(Session.Background Session, LiveTab Tab) : ViewEffect;