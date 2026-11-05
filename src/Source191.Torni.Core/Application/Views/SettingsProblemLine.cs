using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>What a setting's dialog shows under its fields: nothing, or the problem that stopped the last save.</summary>
public abstract record SettingsProblemLine
{
    /// <summary>Nothing is wrong: the problem line is empty.</summary>
    public sealed record None : SettingsProblemLine;

    public sealed record Shows(SettingsProblem Problem) : SettingsProblemLine;
}