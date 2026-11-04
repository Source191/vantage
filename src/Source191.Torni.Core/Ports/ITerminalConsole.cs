using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>
/// The terminal Torni itself runs in: labelling its own tab, and putting the terminal back in order after a child
/// such as claude attach has owned it. Implemented with escape sequences and console APIs at the boundary.
/// </summary>
public interface ITerminalConsole
{
    /// <summary>Titles the tab with Torni's title and colours it.</summary>
    void ShowOwnTab(TabColour colour);

    /// <summary>Titles the tab with Torni's title alone, leaving its colour as it is.</summary>
    void ShowOwnTab();

    /// <summary>Clears the title and resets the tab colour, so the shell's tab looks as it did before Torni ran.</summary>
    void ClearOwnTab();

    /// <summary>Prints the line saying how to come back, just before a child takes the terminal.</summary>
    void ShowReturnHint(string hint);

    /// <summary>Erases the return hint once the child has given the terminal back, so the scrollback keeps no copy.</summary>
    void RemoveReturnHint();

    /// <summary>Resets the terminal modes a child may have left behind and discards its late replies.</summary>
    void RecoverAfterChild();
}