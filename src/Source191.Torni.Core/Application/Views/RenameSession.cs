using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Rename by swapping the job for one resumed under the new name; the session id and conversation stay.</summary>
public sealed record RenameSession(Session.Background Session, SessionName NewName) : ViewEffect;