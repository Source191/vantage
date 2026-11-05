using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Source191.Torni.Infrastructure.Tests;

/// <summary>
/// A sentence a person reads is declared in <c>Application\Wording</c> and its partial files, so a reviewer reads one
/// place for every word. Outside them, a string ending in a full stop, question mark or exclamation mark in
/// <c>Application</c> is a finding, except in the command line's descriptions, the diagnostics and a throw (an
/// exception's message is never user text).
/// </summary>
public sealed class SentenceLiteralsShould
{
    private static readonly string ApplicationFolder = Path.Combine("src", "Source191.Torni.Core", "Application");
    private static readonly char[] SentenceEnds = ['.', '?', '!'];

    [Fact]
    public void KeepSentencesInWording()
    {
        List<string> offenders = [.. SourceFiles.Under(ApplicationFolder)
            .Where(IsCheckedFile)
            .SelectMany(Sentences)];

        offenders.ShouldBeEmpty($"{offenders.Count} sentences are declared outside Wording:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static bool IsCheckedFile(string relativePath)
    {
        string name = Path.GetFileName(relativePath);
        string diagnostics = Path.Combine(ApplicationFolder, "Diagnostics") + Path.DirectorySeparatorChar;

        return !name.StartsWith("Wording", StringComparison.Ordinal)
            && !string.Equals(name, "CommandLine.cs", StringComparison.Ordinal)
            && !relativePath.StartsWith(diagnostics, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Sentences(string relativePath)
    {
        SyntaxNode root = SourceFiles.Parse(SourceFiles.Read(relativePath), relativePath).GetRoot();

        return root.DescendantNodes()
            .Where(IsSentence)
            .Where(node => !node.Ancestors().Any(IsThrow))
            .Select(node => $"{relativePath}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node}");
    }

    private static bool IsThrow(SyntaxNode node) => node is ThrowStatementSyntax or ThrowExpressionSyntax;

    private static bool IsSentence(SyntaxNode node) => node switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => EndsAsASentence(literal.Token.ValueText),
        InterpolatedStringExpressionSyntax interpolated => interpolated.Contents.LastOrDefault() is InterpolatedStringTextSyntax text && EndsAsASentence(text.TextToken.ValueText),
        _ => false,
    };

    private static bool EndsAsASentence(string text) => text.TrimEnd().Length > 0 && SentenceEnds.Contains(text.TrimEnd()[^1]);
}