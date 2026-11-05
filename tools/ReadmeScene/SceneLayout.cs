namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Where the scene's folders are under its home: repositories in <c>source\repos</c>, the console's default root, and
/// worktrees as <c>&lt;repository&gt;-&lt;slug&gt;</c> in <c>source\worktrees</c>, the console's default worktree root beside it.
/// </summary>
public sealed record SceneLayout(string Home)
{
    public string RepositoryRoot => Path.Combine(Home, "source", "repos");

    public string WorktreeRoot => Path.Combine(Home, "source", "worktrees");

    public string Repository(string name) => Path.Combine(RepositoryRoot, name);

    public string Worktree(string repository, string slug) => Path.Combine(WorktreeRoot, $"{repository}-{slug}");

    public string FolderOf(SceneSession session) => session.Worktree.Length == 0 ? Repository(session.Repository) : Worktree(session.Repository, session.Worktree);
}