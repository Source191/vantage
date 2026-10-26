using Source191.Vantage.Domain;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The minimum Claude Code version is one constant; the documents that state it must say the same.</summary>
public sealed class DocumentedMinimumShould
{
    [Theory]
    [InlineData("README.md")]
    [InlineData(@"openspec\config.yaml")]
    public void StateTheMinimumClaudeCodeVersion(string document)
    {
        string text = File.ReadAllText(Path.Combine(RepositoryFolder.Root(), document));

        text.ShouldContain($"{ClaudeCodeSupport.Minimum} or later", customMessage: $"{document} should state the minimum in ClaudeCodeSupport");
    }
}