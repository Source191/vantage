namespace Source191.Torni.ReadmeScene;

/// <summary>A session as the scene lists it, with the process id it is given while it has one.</summary>
internal sealed record ListedSession(SceneSession Session, string Folder, string SessionId, int? ProcessId);