namespace Source191.Torni.Domain;

public enum WorktreeHealthKind
{
    Ok,
    Locked,
    Orphan,
    Broken,
    Empty,

    /// <summary>A live worktree of a repository Torni did not scan, because it is not under the roots.</summary>
    Elsewhere,
}