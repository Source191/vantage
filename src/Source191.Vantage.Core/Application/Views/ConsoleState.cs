namespace Source191.Vantage.Application.Views;

/// <summary>The whole console: every tab's state and which tab shows, kept across tab switches.</summary>
public sealed record ConsoleState(SessionsViewState Sessions, WorktreesViewState Worktrees, ConfigViewState Config, ConsoleTab Active)
{
    public static readonly ConsoleState Empty = new(SessionsViewState.Empty, WorktreesViewState.Empty, ConfigViewState.Empty, ConsoleTab.Sessions);
}