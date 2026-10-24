using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view: the shared table screen run with the worktrees definition.</summary>
public sealed class WorktreesScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    WorktreeActions actions,
    LaunchWorkflow launches,
    WorktreeBase defaultBase,
    TimeProvider clock,
    string context)
    : IWorktreesScreen
{
    public WorktreesScreenExit Run(WorktreesViewState initial)
    {
        TableScreen<WorktreesViewState, WorktreeRow> screen = new(new WorktreesScreenDefinition(actions, launches, defaultBase), load, clock, context);
        ScreenRun<WorktreesViewState> run = screen.Run(initial);

        return new WorktreesScreenExit(run.State, run.Exit);
    }
}