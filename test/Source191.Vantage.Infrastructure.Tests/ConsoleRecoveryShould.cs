using Source191.Vantage.Adapters.Terminal;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class ConsoleRecoveryShould
{
    [Fact]
    public void TurnOffEveryModeAChildCanLeaveBehindAndShowTheCursor()
    {
        using StringWriter console = new();

        ConsoleRecovery.AfterChildProcess(console);

        string written = console.ToString();
        written.ShouldStartWith("\u001b[<u");
        written.ShouldContain("\u001b[?1004l");
        written.ShouldContain("\u001b[?2004l");
        written.ShouldContain("\u001b[?1000l");
        written.ShouldContain("\u001b[?1006l");
        written.ShouldEndWith("\u001b[?25h");
    }

    [Fact]
    public void NotThrowWhenThereIsNoConsoleToFlush()
    {
        using StringWriter console = new();

        Should.NotThrow(() => ConsoleRecovery.AfterChildProcess(console));
    }
}