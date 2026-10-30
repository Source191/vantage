using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>The two tabs of the console.</summary>
public enum ConsoleTab
{
    Sessions,
    Worktrees,
}

/// <summary>The whole console: both views' states and which tab shows, kept across tab switches and hand-offs alike.</summary>
public sealed record ConsoleState(SessionsViewState Sessions, WorktreesViewState Worktrees, ConsoleTab Active)
{
    public static readonly ConsoleState Empty = new(SessionsViewState.Empty, WorktreesViewState.Empty, ConsoleTab.Sessions);
}

/// <summary>
/// The console's own rule: what a tab-switching effect does to the console state. Each view's reducer raises the
/// effect; the console applies it, so neither reducer knows about the other's state.
/// </summary>
public static class ConsoleView
{
    private static readonly HashSet<Type> Handled = [typeof(SwitchToSessions), typeof(SwitchToWorktrees), typeof(JumpToSession)];

    public static ConsoleState Apply(ConsoleState state, ISwitchesTab effect)
    {
        ArgumentNullException.ThrowIfNull(state);

        return effect switch
        {
            SwitchToWorktrees => state with { Active = ConsoleTab.Worktrees },
            SwitchToSessions => state with { Active = ConsoleTab.Sessions },
            JumpToSession jump => state with { Active = ConsoleTab.Sessions, Sessions = SessionsView.Reduce(state.Sessions, new SelectSession(jump.SessionId)).State },
            _ => throw new UnreachableException($"{effect.GetType().Name} switches tab but ConsoleView.Apply has no case for it; EffectRoutingShould guards this."),
        };
    }

    /// <summary>Whether <see cref="Apply"/> has a case for the effect type; the reflection fact asks this for every tab-switching effect.</summary>
    public static bool Handles(Type effectType) => Handled.Contains(effectType);
}