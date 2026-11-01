using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The Windows Terminal operations Vantage needs: open a tab, and select one by title.</summary>
public interface IWindowsTerminal
{
    Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken);

    /// <summary>
    /// Selects the tab titled <paramref name="title"/> in the window named, or in every Windows Terminal window for
    /// <see cref="TerminalWindow.Any"/>. False when no such tab item could be found.
    /// </summary>
    Task<bool> FocusTabAsync(TerminalWindow window, string title, CancellationToken cancellationToken);
}