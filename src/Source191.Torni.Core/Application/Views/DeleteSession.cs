using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>A dialog effect: confirm deleting the session, then ask about its branch when that is not merged.</summary>
public sealed record DeleteSession(Session.Background Session) : ViewEffect, IDialogEffect;