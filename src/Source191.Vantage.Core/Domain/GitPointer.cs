namespace Source191.Vantage.Domain;

/// <summary>
/// What an unregistered folder's .git file says: there is none, the gitdir it points at is gone, the gitdir exists, or
/// the gitdir exists and its repository can be named from the gitdir's shape.
/// </summary>
public abstract record GitPointer
{
    public sealed record NoGitFile : GitPointer;

    public sealed record TargetGone : GitPointer;

    /// <summary>The registration exists, with no owning repository Vantage can name.</summary>
    public sealed record TargetExists : GitPointer;

    /// <summary>
    /// The gitdir exists and names its repository: a linked worktree of a repository outside the roots, a bare
    /// repository's worktree or a <c>--separate-git-dir</c> checkout.
    /// </summary>
    public sealed record RegisteredElsewhere(FolderPath Owner) : GitPointer;
}