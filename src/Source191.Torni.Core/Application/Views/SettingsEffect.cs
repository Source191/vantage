using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>What the dialog does after a reduce: nothing, save these settings, or close unchanged.</summary>
public abstract record SettingsEffect
{
    /// <summary>Nothing: the dialog pushes the new state back to its controls.</summary>
    public sealed record None : SettingsEffect;

    public sealed record Save(Settings Settings) : SettingsEffect;

    public sealed record Close : SettingsEffect;
}