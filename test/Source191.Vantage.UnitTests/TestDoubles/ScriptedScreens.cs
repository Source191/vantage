using Source191.Vantage.Application.Views;

namespace Source191.Vantage.UnitTests.TestDoubles;

/// <summary>A sessions screen that plays back scripted exits, one per run, and keeps the state each run was given.</summary>
public sealed class ScriptedSessionsScreen(params Func<SessionsViewState, ScreenExit>[] runs) : ISessionsScreen
{
    private readonly Queue<Func<SessionsViewState, ScreenExit>> pending = new(runs);

    public List<SessionsViewState> Given { get; } = [];

    public ScreenExit Run(SessionsViewState initial)
    {
        Given.Add(initial);

        return pending.Dequeue()(initial);
    }
}

/// <summary>A worktrees screen that plays back scripted exits, one per run.</summary>
public sealed class ScriptedWorktreesScreen(params ViewEffect?[] exits) : IWorktreesScreen
{
    private readonly Queue<ViewEffect?> pending = new(exits);

    public int Runs { get; private set; }

    public WorktreesScreenExit Run(WorktreesViewState initial)
    {
        Runs++;

        return new WorktreesScreenExit(initial, pending.Dequeue());
    }
}