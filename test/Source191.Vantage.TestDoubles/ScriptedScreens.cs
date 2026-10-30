using Source191.Vantage.Application.Views;

namespace Source191.Vantage.TestDoubles;

/// <summary>A console screen that plays back scripted exits, one per run, and keeps the state each run was given.</summary>
public sealed class ScriptedConsoleScreen(params Func<ConsoleState, ConsoleExit>[] runs) : IConsoleScreen
{
    private readonly Queue<Func<ConsoleState, ConsoleExit>> pending = new(runs);

    public List<ConsoleState> Given { get; } = [];

    public ConsoleExit Run(ConsoleState initial)
    {
        Given.Add(initial);

        return pending.Dequeue()(initial);
    }
}
