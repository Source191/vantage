using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>A tab to open: what it shows, where it starts, and the PowerShell script it runs.</summary>
public sealed record TabLaunch(string Title, TabColour Colour, FolderPath StartingDirectory, string Script);

public sealed class TerminalException(string message) : Exception(message);

/// <summary>The Windows Terminal operations Vantage needs: open a tab, and select one by title.</summary>
public interface IWindowsTerminal
{
    Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken);

    /// <summary>
    /// Selects the tab titled <paramref name="title"/> in the window of the given Windows Terminal process, or in any
    /// Windows Terminal window when the process is unknown. False when no such tab item could be found.
    /// </summary>
    Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken);
}