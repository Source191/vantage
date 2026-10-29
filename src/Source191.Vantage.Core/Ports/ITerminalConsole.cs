using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>
/// The terminal Vantage itself runs in: labelling its own tab, and putting the terminal back in order after a child
/// such as claude attach has owned it. Implemented with escape sequences and console APIs at the boundary.
/// </summary>
public interface ITerminalConsole
{
    /// <summary>Titles the tab with Vantage's title (its name and version) and, when a colour is given, colours it; written again after every hand-off because the child retitles the tab.</summary>
    void ShowOwnTab(TabColour? colour);

    /// <summary>Clears the title and resets the tab colour, so the shell's tab looks as it did before Vantage ran.</summary>
    void ClearOwnTab();

    /// <summary>Prints the line saying how to come back, just before a child takes the terminal.</summary>
    void ShowReturnHint(string hint);

    /// <summary>Erases the return hint once the child has given the terminal back, so the scrollback keeps no copy.</summary>
    void RemoveReturnHint();

    /// <summary>Resets the terminal modes a child may have left behind and discards its late replies.</summary>
    void RecoverAfterChild();
}