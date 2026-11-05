using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Adapters.Terminal;

/// <summary>The console window the program runs in, titled with the given text, through <see cref="ConsoleRecovery"/> and <see cref="TabEscapes"/>.</summary>
public sealed class TerminalConsole(TextWriter console, string title) : ITerminalConsole
{
    public void ShowOwnTab(TabColour colour) => ConsoleRecovery.WriteSequence(console, TabEscapes.Title(title) + TabEscapes.Background(colour));

    public void ShowOwnTab() => ConsoleRecovery.WriteSequence(console, TabEscapes.Title(title));

    public void ClearOwnTab() => ConsoleRecovery.WriteSequence(console, TabEscapes.ClearTitle + TabEscapes.ResetBackground);

    public void ShowReturnHint(string hint) => ConsoleRecovery.WriteSequence(console, hint + console.NewLine);

    public void RemoveReturnHint() => ConsoleRecovery.WriteSequence(console, ConsoleRecovery.EraseLineAbove);

    public void RecoverAfterChild() => ConsoleRecovery.AfterChildProcess(console);
}