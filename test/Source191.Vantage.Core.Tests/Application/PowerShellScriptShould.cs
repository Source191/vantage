using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class PowerShellScriptShould
{
    [Fact]
    public void QuoteTextAsASingleQuotedLiteral()
    {
        PowerShellScript.Quote("plain").ShouldBe("'plain'");
    }

    [Fact]
    public void DoubleEmbeddedSingleQuotesSoTheLiteralStaysIntact()
    {
        PowerShellScript.Quote("Jo's fix").ShouldBe("'Jo''s fix'");
    }

    /// <summary>PowerShell ends a single-quoted string at any of these, so each is doubled with itself to stay literal.</summary>
    [Theory]
    [InlineData("'")]
    [InlineData("‘")]
    [InlineData("’")]
    [InlineData("‚")]
    [InlineData("‛")]
    public void DoubleEveryCharacterPowerShellReadsAsASingleQuote(string quote)
    {
        PowerShellScript.Quote($"O{quote}Neill").ShouldBe($"'O{quote}{quote}Neill'");
    }

    [Fact]
    public void JoinACommandAndItsArgumentsWithTheCallOperator()
    {
        string line = PowerShellScript.Invoke(@"C:\Users\me\.local\bin\claude.exe", ["attach", "81cdb0f9"]);

        line.ShouldBe("& 'C:\\Users\\me\\.local\\bin\\claude.exe' 'attach' '81cdb0f9'");
    }
}