using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Launch;

/// <summary>
/// The repositories the wizard offers: every main checkout directly under the roots saved now, recently used ones
/// first. A linked worktree (a .git file rather than a folder) under a root is not offered, and neither is a root
/// itself: torni here starts a session in any folder.
/// </summary>
public sealed class RepositoryCatalogue(IFolders folders, CurrentSettings settings)
{
    public IReadOnlyList<RepositoryChoice> Choices(IReadOnlyList<FolderPath> recent, string filter)
    {
        List<FolderPath> roots = [.. settings.Value.RepositoryRoots.Where(root => folders.DirectoryExists(root.Value))];
        List<RepositoryChoice> repositories = [.. Paths(roots
            .SelectMany(root => folders.Directories(root.Value))
            .Where(IsMainCheckout))
            .Select(path => new RepositoryChoice(path.Name, path, IsRepository: true))
            .OrderBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)];

        List<RepositoryChoice> recentFirst = [.. recent
            .Select(path => repositories.FirstOrDefault(choice => choice.Path == path))
            .OfType<RepositoryChoice>()];
        IEnumerable<RepositoryChoice> rest = repositories.Where(choice => !recentFirst.Contains(choice));

        return [.. recentFirst.Concat(rest).Where(choice => choice.Matches(filter))];
    }

    private bool IsMainCheckout(string path) => folders.DirectoryExists(System.IO.Path.Combine(path, ".git"));

    /// <summary>The listed and recorded paths that are full paths; anything else is skipped.</summary>
    private static IEnumerable<FolderPath> Paths(IEnumerable<string> texts) => texts.Parsed<FolderPath>();
}