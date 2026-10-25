using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Finds the solution to open for a folder: the .sln and .slnx files directly in it, else directly in its src folder.
/// Several in one place resolve to the one named after the repository when there is exactly one; otherwise the user chooses.
/// The search never descends, so bin, obj and node_modules are never walked.
/// </summary>
public sealed class SolutionFinder(IFolders folders)
{
    public SolutionSearch Find(FolderPath folder, string repositoryName)
    {
        IReadOnlyList<string> found = SolutionsIn(folder);
        if (found.Count == 0)
        {
            found = SolutionsIn(folder.Combine("src"));
        }

        return found switch
        {
            [] => new SolutionSearch.None(),
            [string only] => new SolutionSearch.One(only),
            _ => NamedAfter(found, repositoryName),
        };
    }

    private static SolutionSearch NamedAfter(IReadOnlyList<string> found, string repositoryName)
    {
        IReadOnlyList<string> named = [.. found.Where(file => string.Equals(Path.GetFileNameWithoutExtension(file), repositoryName, StringComparison.OrdinalIgnoreCase))];

        return named.Count == 1 ? new SolutionSearch.One(named[0]) : new SolutionSearch.Several(found);
    }

    private IReadOnlyList<string> SolutionsIn(FolderPath folder)
    {
        return [.. folders.FilesIn(folder.Value)
            .Where(file => Path.GetExtension(file).ToLowerInvariant() is ".sln" or ".slnx")
            .Order(StringComparer.OrdinalIgnoreCase)];
    }
}
