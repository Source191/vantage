using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>The console Vantage runs in, titled with the given text, through <see cref="ConsoleRecovery"/> and <see cref="TabEscapes"/>.</summary>
public sealed class TerminalConsole(TextWriter console, string title) : ITerminalConsole
{
    public void ShowOwnTab(TabColour? colour)
    {
        string label = TabEscapes.Title(title) + (colour is null ? string.Empty : TabEscapes.Background(colour));
        ConsoleRecovery.WriteSequence(console, label);
    }

    public void ClearOwnTab() => ConsoleRecovery.WriteSequence(console, TabEscapes.ClearTitle + TabEscapes.ResetBackground);

    public void ShowReturnHint(string hint) => ConsoleRecovery.WriteSequence(console, hint + console.NewLine);

    public void RemoveReturnHint() => ConsoleRecovery.WriteSequence(console, ConsoleRecovery.EraseLineAbove);

    public void RecoverAfterChild() => ConsoleRecovery.AfterChildProcess(console);
}