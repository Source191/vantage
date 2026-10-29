using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class TerminalConsoleShould : IDisposable
{
    private readonly StringWriter writer = new();

    [Fact]
    public void WriteTheOwnTabTitleAndColourAndClearThemAgain()
    {
        TerminalConsole console = new(writer, "Vantage");

        console.ShowOwnTab(TabPalette.Default[4]);
        console.ShowOwnTab(null);
        console.ClearOwnTab();

        writer.ToString().ShouldBe(
            TabEscapes.Title("Vantage") + TabEscapes.Background(TabPalette.Default[4])
            + TabEscapes.Title("Vantage")
            + TabEscapes.ClearTitle + TabEscapes.ResetBackground);
    }

    [Fact]
    public void WriteTheReturnHintOnItsOwnLineAndEraseItAgain()
    {
        TerminalConsole console = new(writer, "Vantage");

        console.ShowReturnHint();
        console.RemoveReturnHint();

        writer.ToString().ShouldBe("Ctrl+Z returns to Vantage." + Environment.NewLine + "\u001b[1A\u001b[2K");
        ConsoleRecovery.EraseLineAbove.ShouldBe("\u001b[1A\u001b[2K");
    }

    [Fact]
    public void WriteTheTitleItWasGiven()
    {
        TerminalConsole console = new(writer, "Northwind title");

        console.ShowOwnTab(null);

        writer.ToString().ShouldBe(TabEscapes.Title("Northwind title"));
    }

    public void Dispose()
    {
        writer.Dispose();
    }
}