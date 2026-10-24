using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

public sealed record RepositoryChoice(string Name, string Path, bool IsRepository);

/// <summary>
/// The repositories the wizard offers: every git checkout directly under the configured roots, recently used
/// ones first, and each root itself as a "no repository" choice for cross-repository work.
/// </summary>
public sealed class RepositoryCatalogue(IFolders folders, IReadOnlyList<string> roots)
{
    public IReadOnlyList<RepositoryChoice> Choices(IReadOnlyList<string> recent, string filter)
    {
        List<RepositoryChoice> repositories = [.. roots
            .Where(folders.DirectoryExists)
            .SelectMany(folders.Directories)
            .Where(IsGitCheckout)
            .Select(path => new RepositoryChoice(System.IO.Path.GetFileName(path), path, IsRepository: true))
            .OrderBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)];

        List<RepositoryChoice> recentFirst = [.. recent
            .Select(path => repositories.FirstOrDefault(choice => SamePath(choice.Path, path)))
            .Where(choice => choice is not null)
            .Select(choice => choice!)];
        IEnumerable<RepositoryChoice> rest = repositories.Where(choice => !recentFirst.Contains(choice));
        IEnumerable<RepositoryChoice> noRepository = roots
            .Where(folders.DirectoryExists)
            .Select(root => new RepositoryChoice($"{System.IO.Path.GetFileName(root)} (no repository)", root, IsRepository: false));

        return [.. recentFirst.Concat(rest).Concat(noRepository)
            .Where(choice => choice.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))];
    }

    private bool IsGitCheckout(string path)
    {
        string git = System.IO.Path.Combine(path, ".git");

        return folders.DirectoryExists(git) || folders.FileExists(git);
    }

    private static bool SamePath(string left, string right) => string.Equals(left.TrimEnd('\\', '/'), right.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
}