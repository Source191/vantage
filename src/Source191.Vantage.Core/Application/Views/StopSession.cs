using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record StopSession(BackgroundSession Session) : ViewEffect;