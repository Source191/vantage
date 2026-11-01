using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Every C# file under src, test and tools declares one top-level type and is named after it, so a type is found by
/// its name. Nested types stay inside their base, and a partial type's files start with its name.
/// </summary>
public sealed class OneTypePerFileShould
{
    private static readonly string[] Folders = ["src", "test", "tools"];
    private static readonly string[] SkippedFolders = ["bin", "obj"];
    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview);

    [Fact]
    public void HoldOneTypePerFile()
    {
        List<string> offenders = [.. SourceFiles()
            .Where(file => file.Types.Count > 1)
            .Select(file => $"{file.RelativePath} ({file.Types.Count}): {string.Join(", ", file.Types.Select(type => type.Name))}")];

        offenders.ShouldBeEmpty($"{offenders.Count} files declare more than one top-level type:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void NameEachFileAfterItsType()
    {
        List<string> offenders = [.. SourceFiles()
            .Where(file => file.Types.Count == 1 && !file.IsNamedAfter(file.Types[0]))
            .Select(file => $"{file.RelativePath}: {file.Types[0].Name}")];

        offenders.ShouldBeEmpty($"{offenders.Count} files are not named after their type:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static IEnumerable<SourceFile> SourceFiles()
    {
        string root = RepositoryFolder.Root();
        return Folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(root, path))
            .Where(relativePath => !relativePath.Split(Path.DirectorySeparatorChar).Any(segment => SkippedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .Select(relativePath => new SourceFile(relativePath, TopLevelTypes(File.ReadAllText(Path.Combine(root, relativePath)))));
    }

    private static List<TopLevelType> TopLevelTypes(string text)
    {
        CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(text, Options).GetCompilationUnitRoot();
        return [.. TypesAmong(unit.Members)];
    }

    private static IEnumerable<TopLevelType> TypesAmong(SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            IEnumerable<TopLevelType> found = member switch
            {
                BaseNamespaceDeclarationSyntax space => TypesAmong(space.Members),
                BaseTypeDeclarationSyntax type => [new TopLevelType(type.Identifier.Text, HasPartial(type.Modifiers))],
                DelegateDeclarationSyntax type => [new TopLevelType(type.Identifier.Text, HasPartial(type.Modifiers))],
                _ => [],
            };
            foreach (TopLevelType type in found)
            {
                yield return type;
            }
        }
    }

    private static bool HasPartial(SyntaxTokenList modifiers)
    {
        return modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private sealed record TopLevelType(string Name, bool IsPartial);

    private sealed record SourceFile(string RelativePath, List<TopLevelType> Types)
    {
        public bool IsNamedAfter(TopLevelType type)
        {
            string name = Path.GetFileNameWithoutExtension(RelativePath);
            return type.IsPartial
                ? name.StartsWith(type.Name, StringComparison.Ordinal)
                : string.Equals(name, type.Name, StringComparison.Ordinal);
        }
    }
}