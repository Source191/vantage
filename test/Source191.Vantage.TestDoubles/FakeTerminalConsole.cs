using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Records what the loop asks of the terminal Vantage runs in.</summary>
public sealed class FakeTerminalConsole : ITerminalConsole
{
    public List<TabColour?> Shown { get; } = [];

    public int Cleared { get; private set; }

    public int Recovered { get; private set; }

    public void ShowOwnTab(TabColour? colour)
    {
        Shown.Add(colour);
    }

    public void ClearOwnTab()
    {
        Cleared++;
    }

    public void RecoverAfterChild()
    {
        Recovered++;
    }
}