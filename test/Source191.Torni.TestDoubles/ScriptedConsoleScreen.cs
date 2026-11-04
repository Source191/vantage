using Source191.Torni.Application;
using Source191.Torni.Application.Views;

namespace Source191.Torni.TestDoubles;

/// <summary>A console screen that plays back scripted exits, one per run, and keeps the state and the opening dialog each run was given.</summary>
public sealed class ScriptedConsoleScreen(params Func<ConsoleState, ConsoleExit>[] runs) : IConsoleScreen
{
    private readonly Queue<Func<ConsoleState, ConsoleExit>> pending = new(runs);

    public List<ConsoleState> Given { get; } = [];

    public List<WhatsNewAtStart> Openings { get; } = [];

    public ConsoleExit Run(ConsoleState initial, WhatsNewAtStart opening)
    {
        Given.Add(initial);
        Openings.Add(opening);

        return pending.Dequeue()(initial);
    }
}