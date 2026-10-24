using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>The console Vantage runs in, through <see cref="ConsoleRecovery"/> and <see cref="TabEscapes"/>.</summary>
public sealed class TerminalConsole(TextWriter console) : ITerminalConsole
{
    private const string Title = "Vantage";

    public void ShowOwnTab(TabColour? colour)
    {
        string label = TabEscapes.Title(Title) + (colour is null ? string.Empty : TabEscapes.Background(colour));
        ConsoleRecovery.WriteSequence(console, label);
    }

    public void ClearOwnTab() => ConsoleRecovery.WriteSequence(console, TabEscapes.ClearTitle + TabEscapes.ResetBackground);

    public void RecoverAfterChild() => ConsoleRecovery.AfterChildProcess(console);
}