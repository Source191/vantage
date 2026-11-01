#:package Microsoft.CodeAnalysis.CSharp@5.9.0
#:property TreatWarningsAsErrors=false
#:property RunAnalyzers=false
#:property GenerateDocumentationFile=false
#:property InvariantGlobalization=true

// Splits every C# file under src, test and tools that declares more than one top-level type, so each type sits in
// <Name>.cs beside its origin, and renames a single-type file not named after its type with git mv.
// Run from anywhere: dotnet run split-types.cs -- <repository root> [--dry-run]
// The selection rules are OneTypePerFileShould's, copied rather than shared. See design.md.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run split-types.cs -- <repository root> [--dry-run]");
    return 2;
}

string root = Path.GetFullPath(args[0]);
bool dryRun = args.Contains("--dry-run", StringComparer.Ordinal);
string[] folders = ["src", "test", "tools"];
string[] skipped = ["bin", "obj"];
CSharpParseOptions options = new(LanguageVersion.Preview);
UTF8Encoding noBom = new(encoderShouldEmitUTF8Identifier: false);

List<string> files = [.. folders
    .Select(folder => Path.Combine(root, folder))
    .Where(Directory.Exists)
    .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
    .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(segment => skipped.Contains(segment, StringComparer.OrdinalIgnoreCase)))
    .Order(StringComparer.Ordinal)];

List<Split> splits = [];
List<(string From, string To)> renames = [];
List<string> problems = [];
HashSet<string> planned = new(StringComparer.OrdinalIgnoreCase);

foreach (string path in files)
{
    string text = File.ReadAllText(path);
    CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(text, options).GetCompilationUnitRoot();
    List<MemberDeclarationSyntax> types = [.. TopLevelTypes(unit.Members)];
    string fileName = Path.GetFileNameWithoutExtension(path);
    string relative = Path.GetRelativePath(root, path);

    if (types.Count == 1)
    {
        MemberDeclarationSyntax only = types[0];
        if (!IsNamedAfter(fileName, only))
        {
            string target = Path.Combine(Path.GetDirectoryName(path)!, NameOf(only) + ".cs");
            if (File.Exists(target) || !planned.Add(target))
            {
                problems.Add($"clash: renaming {relative} would overwrite {Path.GetRelativePath(root, target)}");
            }

            renames.Add((path, target));
        }

        continue;
    }

    if (types.Count < 2)
    {
        continue;
    }

    MemberDeclarationSyntax? keeper =
        types.FirstOrDefault(type => !IsPartial(type) && string.Equals(NameOf(type), fileName, StringComparison.Ordinal))
        ?? types.FirstOrDefault(type => IsPartial(type) && fileName.StartsWith(NameOf(type), StringComparison.Ordinal));
    List<MemberDeclarationSyntax> moved = [.. types.Where(type => type != keeper)];

    foreach (MemberDeclarationSyntax type in moved)
    {
        if (type.ContainsDirectives)
        {
            problems.Add($"directive: {relative} has a preprocessor directive inside {NameOf(type)}");
        }

        if (type.Parent is NamespaceDeclarationSyntax)
        {
            problems.Add($"block namespace: {relative} declares {NameOf(type)} inside a block-scoped namespace");
        }

        string target = Path.Combine(Path.GetDirectoryName(path)!, NameOf(type) + ".cs");
        if (File.Exists(target) || !planned.Add(target))
        {
            problems.Add($"clash: {NameOf(type)} from {relative} would overwrite {Path.GetRelativePath(root, target)}");
        }
    }

    splits.Add(new Split(path, text, unit, keeper, moved));
}

foreach (Split split in splits)
{
    string keeps = split.Keeper is null ? "deleted" : $"keeps {NameOf(split.Keeper)}";
    Console.WriteLine($"{Path.GetRelativePath(root, split.Path)} ({split.Moved.Count + (split.Keeper is null ? 0 : 1)}): {keeps}; moves {string.Join(", ", split.Moved.Select(NameOf))}");
}

foreach ((string from, string to) in renames)
{
    Console.WriteLine($"rename {Path.GetRelativePath(root, from)} -> {Path.GetFileName(to)}");
}

Console.WriteLine($"{splits.Count} files to split, {splits.Sum(split => split.Moved.Count)} types to move, {splits.Count(split => split.Keeper is null)} files to delete, {renames.Count} renames.");

if (problems.Count > 0)
{
    Console.Error.WriteLine($"Stopped before writing anything; {problems.Count} problems:");
    problems.ForEach(problem => Console.Error.WriteLine("  " + problem));
    return 1;
}

if (dryRun)
{
    return 0;
}

foreach (Split split in splits)
{
    string newline = split.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    bool finalNewline = split.Text.EndsWith('\n');

    foreach (MemberDeclarationSyntax type in split.Moved)
    {
        string content = Header(split.Text, split.Unit, type) + type.ToFullString().TrimStart('\r', '\n', ' ', '\t');
        Write(Path.Combine(Path.GetDirectoryName(split.Path)!, NameOf(type) + ".cs"), content, newline, finalNewline);
    }

    if (split.Keeper is null)
    {
        File.Delete(split.Path);
        continue;
    }

    StringBuilder kept = new(split.Text);
    foreach (MemberDeclarationSyntax type in split.Moved.OrderByDescending(type => type.FullSpan.Start))
    {
        kept.Remove(type.FullSpan.Start, type.FullSpan.Length);
    }

    Write(split.Path, kept.ToString(), newline, finalNewline);
}

foreach ((string from, string to) in renames)
{
    using Process git = Process.Start(new ProcessStartInfo("git", ["-C", root, "mv", from, to]) { UseShellExecute = false })!;
    git.WaitForExit();
    if (git.ExitCode != 0)
    {
        Console.Error.WriteLine($"git mv {from} {to} failed with {git.ExitCode}");
        return 1;
    }
}

Console.WriteLine("Done. Next: dotnet format style --diagnostics IDE0005 --severity warn, then dotnet format whitespace and dotnet build -c Debug.");
return 0;

IEnumerable<MemberDeclarationSyntax> TopLevelTypes(SyntaxList<MemberDeclarationSyntax> members)
{
    foreach (MemberDeclarationSyntax member in members)
    {
        if (member is BaseNamespaceDeclarationSyntax space)
        {
            foreach (MemberDeclarationSyntax inner in TopLevelTypes(space.Members))
            {
                yield return inner;
            }
        }
        else if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        {
            yield return member;
        }
    }
}

static string NameOf(MemberDeclarationSyntax type) => type switch
{
    BaseTypeDeclarationSyntax declaration => declaration.Identifier.Text,
    DelegateDeclarationSyntax declaration => declaration.Identifier.Text,
    _ => throw new UnreachableException(),
};

static bool IsPartial(MemberDeclarationSyntax type) => type.Modifiers.Any(SyntaxKind.PartialKeyword);

static bool IsNamedAfter(string fileName, MemberDeclarationSyntax type) => IsPartial(type)
    ? fileName.StartsWith(NameOf(type), StringComparison.Ordinal)
    : string.Equals(fileName, NameOf(type), StringComparison.Ordinal);

// The origin's using directives and anything else above its namespace, then the file-scoped namespace line and a
// blank line; for a type in the global namespace, the text above the first member.
static string Header(string text, CompilationUnitSyntax unit, MemberDeclarationSyntax type)
{
    if (type.Parent is FileScopedNamespaceDeclarationSyntax space)
    {
        string above = text[..space.SpanStart];
        string line = text[space.SpanStart..space.SemicolonToken.Span.End];
        return above + line + Environment.NewLine + Environment.NewLine;
    }

    int first = unit.Members.First().FullSpan.Start;
    string usings = text[..first].TrimEnd('\r', '\n', ' ', '\t');
    return usings.Length == 0 ? string.Empty : usings + Environment.NewLine + Environment.NewLine;
}

void Write(string path, string content, string newline, bool finalNewline)
{
    string normalised = content.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n', ' ', '\t');
    normalised = normalised.Replace("\n", newline, StringComparison.Ordinal) + (finalNewline ? newline : string.Empty);
    File.WriteAllText(path, normalised, noBom);
}

sealed record Split(string Path, string Text, CompilationUnitSyntax Unit, MemberDeclarationSyntax? Keeper, List<MemberDeclarationSyntax> Moved);
