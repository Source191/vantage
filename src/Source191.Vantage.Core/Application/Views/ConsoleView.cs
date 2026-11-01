using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
    private static readonly HashSet<Type> Handled = [typeof(SelectTab), typeof(NextTab), typeof(PreviousTab)];

    public static ConsoleState Apply(ConsoleState state, ISwitchesTab effect)
    {
        ArgumentNullException.ThrowIfNull(state);

        return effect switch
        {
            SelectTab select => state with { Active = select.Tab },
            NextTab => state with { Active = state.Active == ConsoleTab.Config ? ConsoleTab.Config : state.Active + 1 },
            PreviousTab => state with { Active = state.Active == ConsoleTab.Sessions ? ConsoleTab.Sessions : state.Active - 1 },
            _ => throw new UnreachableException($"{effect.GetType().Name} switches tab but ConsoleView.Apply has no case for it; EffectRoutingShould guards this."),
        };
    }

    /// <summary>
    /// Enter on a worktree: what Enter on its session's row would do, found among all the Sessions tab's rows (a filter there
    /// hides nothing here) by id, or why nothing opens. No tab's state changes; the worktrees tab runs the effect or shows the message.
    /// </summary>
    public static RowDecision OpenSession(ConsoleState state, OpenWorktreeSession open)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(open);

        return state.Sessions.Rows.FirstOrDefault(row => row.Session.SessionId == open.SessionId) switch
        {
            null => new RowDecision.Explain(new StatusMessage.SessionNotListed(open.Name)),
            SessionRow row => SessionsView.Opening(row),
        };
    }

    /// <summary>Whether <see cref="OpenSession"/> answers the effect type; the reflection fact asks this for every effect that consults the sessions.</summary>
    public static bool Answers(Type effectType) => effectType == typeof(OpenWorktreeSession);

    /// <summary>Whether <see cref="Apply"/> has a case for the effect type; the reflection fact asks this for every tab-switching effect.</summary>
    public static bool Handles(Type effectType) => Handled.Contains(effectType);
}
/// <summary>The keys every tab shares for moving between tabs: 1, 2 and 3 choose one, Left and Right step through them.</summary>
public static class TabKeys
{
    /// <summary>The effect of a tab key; false for a key that is not one.</summary>
    public static bool TryFor(Key key, [NotNullWhen(true)] out ViewEffect? effect)
    {
        ArgumentNullException.ThrowIfNull(key);
        effect = key switch
        {
            { Name: KeyName.Left } => new PreviousTab(),
            { Name: KeyName.Right } => new NextTab(),
            { Name: KeyName.Character, Character: '1' } => new SelectTab(ConsoleTab.Sessions),
            { Name: KeyName.Character, Character: '2' } => new SelectTab(ConsoleTab.Worktrees),
            { Name: KeyName.Character, Character: '3' } => new SelectTab(ConsoleTab.Config),
            _ => null,
        };

        return effect is not null;
    }
}