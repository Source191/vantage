using System.Diagnostics.CodeAnalysis;

namespace Source191.Torni.Application.Views;

/// <summary>The keys every tab answers the same way: q quits, ? helps, U updates and ! reports a problem.</summary>
public static class SharedKeys
{
    /// <summary>The effect of a shared key; false for a key that is not one.</summary>
    public static bool TryFor(Key key, [MaybeNullWhen(false)] out ViewEffect effect)
    {
        effect = key switch
        {
            { Name: KeyName.Character, Character: 'q' } => new Quit(),
            { Name: KeyName.Character, Character: '?' } => new ShowHelp(),
            { Name: KeyName.Character, Character: 'U' } => new SelfUpdate(),
            { Name: KeyName.Character, Character: '!' } => new ShowProblemReport(),
            _ => null,
        };

        return effect is not null;
    }
}