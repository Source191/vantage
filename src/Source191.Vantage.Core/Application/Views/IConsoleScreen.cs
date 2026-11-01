namespace Source191.Vantage.Application.Views;

/// <summary>A full-screen run of the console, every tab in one application: keys go to the active tab's reducer until <see cref="Quit"/>.</summary>
public interface IConsoleScreen
{
    ConsoleExit Run(ConsoleState initial);
}