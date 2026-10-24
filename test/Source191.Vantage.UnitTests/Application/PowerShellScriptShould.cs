using Source191.Vantage.Application;

namespace Source191.Vantage.UnitTests.Application;

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

    [Fact]
    public void JoinACommandAndItsArgumentsWithTheCallOperator()
    {
        string line = PowerShellScript.Invoke(@"C:\Users\me\.local\bin\claude.exe", ["attach", "81cdb0f9"]);

        line.ShouldBe("& 'C:\\Users\\me\\.local\\bin\\claude.exe' 'attach' '81cdb0f9'");
    }
}