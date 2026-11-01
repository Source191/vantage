using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

/// <summary>
/// What may be handed to a batch file:
/// <see href="https://learn.microsoft.com/windows-server/administration/windows-commands/cmd">cmd.exe reads its command
/// line again</see>, so only plain text goes through.
/// </summary>
public sealed class BatchFileArgumentShould
{
    [Theory]
    [InlineData("fix-123")]
    [InlineData("feature/refund-retry")]
    [InlineData("Northwind Trading")]
    [InlineData("Northwind.Orders_v2")]
    public void AcceptLettersDigitsSpaceDotUnderscoreHyphenAndSlash(string text)
    {
        BatchFileArgument.IsSafe(text).ShouldBeTrue();
    }

    [Theory]
    [InlineData("feature/x&calc")]
    [InlineData("a|b")]
    [InlineData("%PATH%")]
    [InlineData("say \"hi\"")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a^b")]
    [InlineData("hi!")]
    [InlineData("")]
    public void RefuseAnythingCmdExeReadsSpecially(string text)
    {
        BatchFileArgument.IsSafe(text).ShouldBeFalse();
    }
}