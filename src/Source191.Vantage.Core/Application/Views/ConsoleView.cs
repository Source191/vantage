using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>The tabs of the console, in the order they sit in the strip.</summary>
public enum ConsoleTab
{
    Sessions,
    Worktrees,
    Config,
}

/// <summary>Show this tab: 1, 2 and 3, a click on its title, and Esc back to Sessions.</summary>
public sealed record SelectTab(ConsoleTab Tab) : ViewEffect, ISwitchesTab;

/// <summary>Right: the tab after the one shown, stopping at the last.</summary>
public sealed record NextTab : ViewEffect, ISwitchesTab;

/// <summary>Left: the tab before the one shown, stopping at the first.</summary>
public sealed record PreviousTab : ViewEffect, ISwitchesTab;

/// <summary>The whole console: every tab's state and which tab shows, kept across tab switches and hand-offs alike.</summary>
public sealed record ConsoleState(SessionsViewState Sessions, WorktreesViewState Worktrees, ConfigViewState Config, ConsoleTab Active)
{
    public static readonly ConsoleState Empty = new(SessionsViewState.Empty, WorktreesViewState.Empty, ConfigViewState.Empty, ConsoleTab.Sessions);
}

/// <summary>
/// The console's own rule: what a tab-switching effect does to the console state. Each view's reducer raises the
/// effect; the console applies it, so neither reducer knows about the other's state.
/// </summary>
public static class ConsoleView
{
    private static readonly HashSet<Type> Handled = [typeof(SwitchToSessions), typeof(SwitchToWorktrees), typeof(JumpToSession), typeof(SelectTab), typeof(NextTab), typeof(PreviousTab)];

    public static ConsoleState Apply(ConsoleState state, ISwitchesTab effect)
    {
        ArgumentNullException.ThrowIfNull(state);

        return effect switch
        {
            SwitchToWorktrees => state with { Active = ConsoleTab.Worktrees },
            SwitchToSessions => state with { Active = ConsoleTab.Sessions },
            SelectTab or NextTab or PreviousTab => state,
            JumpToSession jump => state with { Active = ConsoleTab.Sessions, Sessions = SessionsView.Reduce(state.Sessions, new SelectSession(jump.SessionId)).State },
            _ => throw new UnreachableException($"{effect.GetType().Name} switches tab but ConsoleView.Apply has no case for it; EffectRoutingShould guards this."),
        };
    }

    /// <summary>Whether <see cref="Apply"/> has a case for the effect type; the reflection fact asks this for every tab-switching effect.</summary>
    public static bool Handles(Type effectType) => Handled.Contains(effectType);
}