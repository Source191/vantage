namespace Source191.Vantage.Domain;

public enum WorktreeHealthKind
{
    Ok,
    Locked,
    Orphan,
    Broken,
    Empty,

    /// <summary>A live worktree of a repository Vantage did not scan, because it is not under the roots.</summary>
    Elsewhere,
}