namespace Source191.Vantage.Application.Views;

/// <summary>A full-screen run of the console, every tab in one application: keys go to the active tab's reducer until <see cref="Quit"/>.</summary>
public interface IConsoleScreen
{
    /// <summary>Runs the console from <paramref name="initial"/>, first showing <paramref name="opening"/>'s What's new dialog, before any key reaches a tab, when it has one.</summary>
    ConsoleExit Run(ConsoleState initial, WhatsNewAtStart opening);
}