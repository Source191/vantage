using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

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