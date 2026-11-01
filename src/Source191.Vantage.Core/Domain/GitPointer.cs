namespace Source191.Vantage.Domain;

/// <summary>
/// What an unregistered folder's .git file says: there is none, the registration it points at is gone, the registration
/// exists, or it exists in a repository that still has its .git folder, which is named.
/// </summary>
public abstract record GitPointer
{
    public sealed record NoGitFile : GitPointer;

    public sealed record TargetGone : GitPointer;

    /// <summary>The registration exists, with no owning repository Vantage can name.</summary>
    public sealed record TargetExists : GitPointer;

    /// <summary>The registration exists in a repository that still has its .git folder.</summary>
    public sealed record RegisteredElsewhere(FolderPath Owner) : GitPointer;
}