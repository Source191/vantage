using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Move a session started as plain claude in another tab under the supervisor, so the console can manage it.</summary>
public sealed record AdoptSession(Session.Interactive Session) : ViewEffect;