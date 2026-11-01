using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Application.Views;

/// <summary>The keys every tab shares for moving between tabs: 1, 2 and 3 choose one, Left and Right step through them.</summary>
public static class TabKeys
{
    /// <summary>The effect of a tab key; false for a key that is not one.</summary>
    public static bool TryFor(Key key, [MaybeNullWhen(false)] out ViewEffect effect)
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