using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Application.Views;

/// <summary>The keys every tab shares for moving between tabs: Left and Right step through them.</summary>
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
            _ => null,
        };

        return effect is not null;
    }
}