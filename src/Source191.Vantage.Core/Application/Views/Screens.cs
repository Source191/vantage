namespace Source191.Vantage.Application.Views;

/// <summary>What the sessions screen hands back when it stops: the state to resume with and the effect that needs the terminal or another screen.</summary>
public sealed record ScreenExit(SessionsViewState State, ViewEffect? Effect);

/// <summary>What the worktrees screen hands back when it stops.</summary>
public sealed record WorktreesScreenExit(WorktreesViewState State, ViewEffect? Effect);

/// <summary>A full-screen run of the sessions view: keys go to the reducer until an effect needs the caller.</summary>
public interface ISessionsScreen
{
    ScreenExit Run(SessionsViewState initial);
}

/// <summary>A full-screen run of the worktrees view.</summary>
public interface IWorktreesScreen
{
    WorktreesScreenExit Run(WorktreesViewState initial);
}