using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class TerminalConsoleShould : IDisposable
{
    private readonly StringWriter writer = new();

    [Fact]
    public void WriteTheOwnTabTitleAndColourAndClearThemAgain()
    {
        TerminalConsole console = new(writer, "Vantage (0.5.0)");

        console.ShowOwnTab(TabPalette.Default[4]);
        console.ShowOwnTab(null);
        console.ClearOwnTab();

        writer.ToString().ShouldBe(
            TabEscapes.Title("Vantage (0.5.0)") + TabEscapes.Background(TabPalette.Default[4])
            + TabEscapes.Title("Vantage (0.5.0)")
            + TabEscapes.ClearTitle + TabEscapes.ResetBackground);
    }

    [Fact]
    public void WriteTheTitleItWasGiven()
    {
        TerminalConsole console = new(writer, "Vantage (0.5.0-local)");

        console.ShowOwnTab(null);

        writer.ToString().ShouldBe(TabEscapes.Title("Vantage (0.5.0-local)"));
    }

    public void Dispose()
    {
        writer.Dispose();
    }
}