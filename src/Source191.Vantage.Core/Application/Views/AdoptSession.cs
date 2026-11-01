using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Move a session started as plain claude in another tab under the supervisor, so Vantage can manage it.</summary>
public sealed record AdoptSession(InteractiveSession Session) : ViewEffect;