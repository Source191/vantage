using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view: the shared table screen run with the worktrees definition.</summary>
public sealed class WorktreesScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    DialogEffects dialogs,
    EffectDispatcher dispatcher,
    TimeProvider clock,
    string context)
    : IWorktreesScreen
{
    public WorktreesScreenExit Run(WorktreesViewState initial)
    {
        TableScreen<WorktreesViewState, WorktreeRow> screen = new(new WorktreesScreenDefinition(), dialogs, dispatcher, load, clock, context);
        ScreenRun<WorktreesViewState> run = screen.Run(initial);

        return new WorktreesScreenExit(run.State, run.Exit);
    }
}