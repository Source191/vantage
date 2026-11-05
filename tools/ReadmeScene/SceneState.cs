namespace Source191.Torni.ReadmeScene;

/// <summary>What a scene session is doing, each one a combination of the listing's pid, status and state.</summary>
public enum SceneState
{
    Starting,
    Working,
    Waiting,
    Idle,
    Done,
    Failed,
    Stopped,
}