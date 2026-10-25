using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

public sealed record RepositoryChoice(string Name, FolderPath Path, bool IsRepository)
{
    /// <summary>Whether the filter typed in the wizard keeps this choice: part of its name, in any case.</summary>
    public bool Matches(string filter) => Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// The repositories the wizard offers: every git checkout directly under the configured roots, recently used
/// ones first, and each root itself as a "no repository" choice for cross-repository work.
/// </summary>
public sealed class RepositoryCatalogue(IFolders folders, IReadOnlyList<string> roots)
{
    public IReadOnlyList<RepositoryChoice> Choices(IReadOnlyList<string> recent, string filter)
    {
        List<RepositoryChoice> repositories = [.. Paths(roots
            .Where(folders.DirectoryExists)
            .SelectMany(folders.Directories)
            .Where(IsGitCheckout))
            .Select(path => new RepositoryChoice(path.Name, path, IsRepository: true))
            .OrderBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)];

        List<RepositoryChoice> recentFirst = [.. Paths(recent)
            .Select(path => repositories.FirstOrDefault(choice => choice.Path == path))
            .OfType<RepositoryChoice>()];
        IEnumerable<RepositoryChoice> rest = repositories.Where(choice => !recentFirst.Contains(choice));
        IEnumerable<RepositoryChoice> noRepository = Paths(roots.Where(folders.DirectoryExists))
            .Select(root => new RepositoryChoice($"{root.Name} (no repository)", root, IsRepository: false));

        return [.. recentFirst.Concat(rest).Concat(noRepository)
            .Where(choice => choice.Matches(filter))];
    }

    private bool IsGitCheckout(string path)
    {
        string git = System.IO.Path.Combine(path, ".git");

        return folders.DirectoryExists(git) || folders.FileExists(git);
    }

    /// <summary>The configured, listed and recorded paths that are full paths; anything else is skipped.</summary>
    private static IEnumerable<FolderPath> Paths(IEnumerable<string> texts) => texts.Parsed<FolderPath>();
}