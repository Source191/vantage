namespace Source191.Torni.ReadmeScene;

/// <summary>An Agent SDK or <c>claude -p</c> run, which the console counts but never lists as a session.</summary>
public sealed record SceneAutomatedRun(string Repository, int StartedMinutesAgo, int MemoryMegabytes = 0)
{
    public DateTimeOffset StartedAt(TimeProvider clock) => clock.GetUtcNow().AddMinutes(-StartedMinutesAgo);
}