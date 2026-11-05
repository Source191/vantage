namespace Source191.Torni.ReadmeScene;

/// <summary>Everything the README's screenshot shows, read from <c>scene.json</c> by <see cref="SceneFile"/>.</summary>
public sealed record Scene(
    string ClaudeVersion,
    IReadOnlyList<SceneRepository> Repositories,
    IReadOnlyList<SceneSession> Sessions,
    IReadOnlyList<SceneAutomatedRun> AutomatedRuns,
    IReadOnlyList<ScenePullRequest> PullRequests);