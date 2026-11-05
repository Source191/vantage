namespace Source191.Torni.ReadmeScene;

/// <summary>
/// One session row: where it runs (the repository's main checkout when <see cref="Worktree"/> is blank, otherwise the
/// worktree with that slug), what Claude Code says it is doing, and whether the console opens a tab for it.
/// </summary>
public sealed record SceneSession(
    string Name,
    string Repository,
    SceneKind Kind,
    SceneState State,
    int StartedMinutesAgo,
    string Worktree = "",
    string WaitingFor = "",
    bool Tab = false,
    int MemoryMegabytes = 0)
{
    public DateTimeOffset StartedAt(TimeProvider clock) => clock.GetUtcNow().AddMinutes(-StartedMinutesAgo);
}