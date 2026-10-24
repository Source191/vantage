using IOPath = System.IO.Path;

namespace Source191.Vantage.Domain;

public sealed record SiblingWorktree(string Path, string Branch)
{
    public static SiblingWorktree For(RepositoryPath repository, WorktreeSlug slug)
    {
        if (repository.Parent.Length == 0)
        {
            throw new ArgumentException($"{repository.Value} has no parent folder for a sibling worktree to sit in.", nameof(repository));
        }

        string path = IOPath.Combine(repository.Parent, $"{repository.Name}-{slug.Value}");

        return new SiblingWorktree(path, slug.Value);
    }
}