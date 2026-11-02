using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The repository's C# files under some folders, read and parsed the same way for every source-text fact.</summary>
internal static class SourceFiles
{
    private static readonly string[] SkippedFolders = ["bin", "obj"];
    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview);

    /// <summary>Every C# file below the named folders of the repository, in path order, outside bin and obj.</summary>
    public static IEnumerable<string> Under(params string[] folders)
    {
        string root = RepositoryFolder.Root();
        return folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(root, path))
            .Where(relativePath => !relativePath.Split(Path.DirectorySeparatorChar).Any(segment => SkippedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal);
    }

    public static string Read(string relativePath) => File.ReadAllText(Path.Combine(RepositoryFolder.Root(), relativePath));

    public static SyntaxTree Parse(string text, string relativePath = "") => CSharpSyntaxTree.ParseText(text, Options, relativePath);
}