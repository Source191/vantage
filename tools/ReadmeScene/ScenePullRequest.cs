namespace Source191.Torni.ReadmeScene;

/// <summary>A pull request for one branch of a demo repository, answered as gh or az would by the host its remote names.</summary>
public sealed record ScenePullRequest(string Repository, string Branch, int Number, ScenePullRequestState State, int OpenedDaysAgo)
{
    public DateTimeOffset OpenedAt(TimeProvider clock) => clock.GetUtcNow().AddDays(-OpenedDaysAgo);
}