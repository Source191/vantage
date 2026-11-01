using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>A dialog effect: confirm deleting the session, then ask about its branch when that is not merged.</summary>
public sealed record DeleteSession(BackgroundSession Session) : ViewEffect, IDialogEffect;