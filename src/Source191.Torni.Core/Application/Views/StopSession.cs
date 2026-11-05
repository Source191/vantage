using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

public sealed record StopSession(Session.Background Session) : ViewEffect;