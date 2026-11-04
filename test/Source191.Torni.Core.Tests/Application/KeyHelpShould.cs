using Source191.Torni.Application;
using Source191.Torni.Application.Views;

namespace Source191.Torni.Core.Tests.Application;

/// <summary>A key of a view: on the key bar with its hint, or only in the help.</summary>
public sealed class KeyHelpShould
{
    [Fact]
    public void PutAHelpOnlyKeyOffTheBar()
    {
        KeyHelp help = KeyHelp.HelpOnly(Key.Of('1'), "the Sessions tab", "Show the Sessions tab.");

        help.Hint.ShouldBeEmpty();
        help.OnBar.ShouldBeFalse();
        help.Line.ShouldBe("the Sessions tab");
        help.Tip.ShouldBe("Show the Sessions tab.");
    }

    [Fact]
    public void KeepABarKeyOnTheBar()
    {
        KeyHelp help = new(Key.Of('n'), "New session", "new session", "Start a new session.");

        help.Hint.ShouldBe("New session");
        help.OnBar.ShouldBeTrue();
    }

    [Fact]
    public void CarryTheKeyItStandsFor()
    {
        KeyHelp help = new(Key.Enter, "Open", "open it", "Open the row.");

        help.Press.ShouldBe(Key.Enter);
        help.Key.ShouldBe("enter");
        help.HelpKey.ShouldBe("Enter");
    }

    [Theory]
    [InlineData(KeyName.Escape, "esc", "Esc")]
    [InlineData(KeyName.Left, "←", "←")]
    [InlineData(KeyName.Right, "→", "→")]
    [InlineData(KeyName.Up, "↑", "↑")]
    [InlineData(KeyName.Down, "↓", "↓")]
    [InlineData(KeyName.Backspace, "backspace", "Backspace")]
    public void WriteEveryNamedKeyAsTheHelpDoes(KeyName name, string key, string helpKey)
    {
        KeyHelp help = KeyHelp.HelpOnly(new Key(name, '\0'), "line", "tip");

        help.Key.ShouldBe(key);
        help.HelpKey.ShouldBe(helpKey);
    }

    [Fact]
    public void WriteACharacterKeyAsTyped()
    {
        KeyHelp help = KeyHelp.HelpOnly(Key.Of('D'), "line", "tip");

        help.Key.ShouldBe("D");
        help.HelpKey.ShouldBe("D");
    }
}