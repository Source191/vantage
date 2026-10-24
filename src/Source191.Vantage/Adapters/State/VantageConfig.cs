namespace Source191.Vantage.Adapters.State;

/// <summary>User configuration kept beside the state: where repositories live and how new worktrees branch.</summary>
public sealed record VantageConfig(IReadOnlyList<string> RepositoryRoots, string DefaultBase)
{
    public static VantageConfig Default => new(
        [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos")],
        DefaultBase: "RemoteDefault");

    public bool Equals(VantageConfig? other)
    {
        return other is not null
            && DefaultBase == other.DefaultBase
            && RepositoryRoots.SequenceEqual(other.RepositoryRoots);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DefaultBase, RepositoryRoots.Count);
    }
}