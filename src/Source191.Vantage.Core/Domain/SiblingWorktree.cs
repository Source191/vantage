namespace Source191.Vantage.Domain;

public sealed record SiblingWorktree(FolderPath Path, string Branch)
{
    public static SiblingWorktree For(FolderPath repository, WorktreeSlug slug)
    {
        if (repository.Parent is not FolderPath parent)
        {
            throw new ArgumentException($"{repository.Value} has no parent folder for a sibling worktree to sit in.", nameof(repository));
        }

        return new SiblingWorktree(parent.Combine($"{repository.Name}-{slug.Value}"), slug.Value);
    }
}