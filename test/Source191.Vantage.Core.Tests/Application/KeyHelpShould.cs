using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>A key of a view: on the key bar with its hint, or only in the help.</summary>
public sealed class KeyHelpShould
{
    [Fact]
    public void PutAHelpOnlyKeyOffTheBar()
    {
        KeyHelp help = KeyHelp.HelpOnly("1", "the Sessions tab", "Show the Sessions tab.");

        help.Hint.ShouldBeEmpty();
        help.OnBar.ShouldBeFalse();
        help.Line.ShouldBe("the Sessions tab");
        help.Tip.ShouldBe("Show the Sessions tab.");
    }

    [Fact]
    public void KeepABarKeyOnTheBar()
    {
        KeyHelp help = new("n", "New session", "new session", "Start a new session.");

        help.Hint.ShouldBe("New session");
        help.OnBar.ShouldBeTrue();
    }
}