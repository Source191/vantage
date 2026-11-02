using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// The conventions CLAUDE.md states for the code under src that no analyser holds, read from the syntax tree so a comment,
/// a string holding <c>!=</c> or a doc comment naming a keyword is not mistaken for code.
/// </summary>
public sealed class SourceConventionsShould
{
    private const string ApplicationFolder = @"src\Source191.Vantage.Core\Application";
    private const string SharedSnapshotFile = @"src\Source191.Vantage.Core\Application\SharedSnapshot.cs";

    private static readonly string[] ArgumentExceptionFiles =
    [
        @"src\Source191.Vantage.Infrastructure\Claude\ClaudeSettings.cs",
        @"src\Source191.Vantage.Infrastructure\Processes\ProcessRegistry.cs",
    ];

    private static readonly string[] AmericanWords =
    [
        "color", "colors", "colored", "behavior", "center", "centered", "canceled", "favorite", "gray", "initialize",
        "initialized", "organize", "analyze", "catalog", "normalize", "recognize", "customize", "disk",
    ];

    private static readonly Regex AmericanWord = new($@"\b({string.Join('|', AmericanWords)})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    [Fact]
    public void UseNoNullForgivingOperator()
    {
        List<string> offenders = [.. Find(["src"], item => item.AsNode() is PostfixUnaryExpressionSyntax postfix && postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression)).Select(Describe)];

        offenders.ShouldBeEmpty($"{offenders.Count} uses of the null-forgiving operator:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void KeepVolatileInsideSharedSnapshot()
    {
        List<string> files = [.. Find(["src"], item => item.IsToken && item.IsKind(SyntaxKind.VolatileKeyword)).Select(found => found.RelativePath).Distinct(StringComparer.Ordinal)];

        files.ShouldBe([SharedSnapshotFile]);
    }

    [Fact]
    public void CatchArgumentExceptionOnlyWhereNamed()
    {
        List<string> files = [.. Find(["src"], item => item.AsNode() is CatchDeclarationSyntax { Type: var type } && type.ToString() is "ArgumentException" or "System.ArgumentException")
            .Select(found => found.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        files.ShouldBe([.. ArgumentExceptionFiles.Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public void SpellBritish()
    {
        List<string> offenders = [.. Find(["src"], IsWordingText)
            .SelectMany(found => AmericanWord.Matches(found.Text).Select(match => $"{Describe(found)}: {match.Value}"))];

        offenders.ShouldBeEmpty($"{offenders.Count} American spellings in Wording and the command line:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static bool IsWordingText(SyntaxNodeOrToken item)
    {
        bool isText = item.IsToken && (item.IsKind(SyntaxKind.StringLiteralToken) || item.IsKind(SyntaxKind.InterpolatedStringTextToken) || item.IsKind(SyntaxKind.Utf8StringLiteralToken));
        string? file = item.SyntaxTree?.FilePath;

        return isText && file is not null && IsWordingFile(file);
    }

    private static bool IsWordingFile(string relativePath)
    {
        string name = Path.GetFileName(relativePath);
        bool inApplication = string.Equals(Path.GetDirectoryName(relativePath), ApplicationFolder, StringComparison.Ordinal);

        return inApplication && (name == "CommandLine.cs" || name.StartsWith("Wording", StringComparison.Ordinal));
    }

    private static string Describe(Found found) => $"{found.RelativePath}({found.Line})";

    private static IEnumerable<Found> Find(string[] folders, Func<SyntaxNodeOrToken, bool> matches)
    {
        foreach (string relativePath in SourceFiles.Under(folders))
        {
            SyntaxTree tree = SourceFiles.Parse(SourceFiles.Read(relativePath), relativePath);
            foreach (SyntaxNodeOrToken item in tree.GetRoot().DescendantNodesAndTokens().Where(matches))
            {
                yield return new Found(relativePath, item.GetLocation()?.GetLineSpan().StartLinePosition.Line + 1 ?? 0, item.ToString());
            }
        }
    }

    private sealed record Found(string RelativePath, int Line, string Text);
}