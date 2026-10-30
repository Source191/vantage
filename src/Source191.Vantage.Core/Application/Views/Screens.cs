namespace Source191.Vantage.Application.Views;

/// <summary>What the console screen hands back when it stops: the state to resume with and the effect that needs the terminal, or null or <see cref="Quit"/> to exit.</summary>
public sealed record ConsoleExit(ConsoleState State, ViewEffect? Effect);

/// <summary>A full-screen run of the console, both tabs in one application: keys go to the active tab's reducer until an effect needs the caller.</summary>
public interface IConsoleScreen
{
    ConsoleExit Run(ConsoleState initial);
}