namespace Source191.Vantage.Application.Views;

/// <summary>A full-screen run of the console, both tabs in one application: keys go to the active tab's reducer until an effect needs the caller.</summary>
public interface IConsoleScreen
{
    ConsoleExit Run(ConsoleState initial);
}