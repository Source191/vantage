namespace Source191.Torni.ReadmeScene;

/// <summary>
/// What the console plays once it is showing: the keys that set the scene up, then the keys to record, with the
/// folder their frames go to (blank to record nothing).
/// </summary>
internal sealed record ScenePlay(IReadOnlyList<KeyStep> Setup, IReadOnlyList<KeyStep> Recorded, string FramesFolder);