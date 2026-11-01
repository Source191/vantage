namespace Source191.Vantage.Domain;

/// <summary>
/// What a checkout's HEAD is on, as git said it: a branch, a detached commit, or nothing git named (a leftover folder
/// git no longer lists, a session outside a repository or whose branch git did not give, or branch text that is not a
/// valid name). A named case stands where a null branch would, so every action on a branch matches <see cref="OnBranch"/>.
/// </summary>
public abstract record Head
{
    public sealed record OnBranch(BranchName Branch) : Head;

    /// <summary>Git's listing says the worktree is on a detached head.</summary>
    public sealed record Detached : Head;

    /// <summary>No head git named: not known to be on a branch, nor known to be detached.</summary>
    public sealed record Unknown : Head;
}