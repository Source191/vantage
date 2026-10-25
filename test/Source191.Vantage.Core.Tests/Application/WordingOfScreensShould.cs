using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfScreensShould
{
    [Fact]
    public void TitleTheTabWithTheVersion()
    {
        Wording.TabTitle(new ToolVersion("0.5.0")).ShouldBe("Vantage (0.5.0)");
        Wording.TabTitle(new ToolVersion("0.5.0-local")).ShouldBe("Vantage (0.5.0-local)");
    }

    [Fact]
    public void ShowVersionsAndRootsInTheHeader()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0"), "2.1.310", [@"C:\Users\me\source\repos", @"D:\work"])
            .ShouldBe("0.5.0\n2.1.310\nC:\\Users\\me\\source\\repos; D:\\work");
    }

    [Fact]
    public void ShowAnUnreadableClaudeVersionAsUnknown()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0-local"), claudeVersion: null, [@"C:\Users\me\source\repos"])
            .ShouldBe("0.5.0-local\nunknown\nC:\\Users\\me\\source\\repos");
    }
}