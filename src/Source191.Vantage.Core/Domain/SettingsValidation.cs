namespace Source191.Vantage.Domain;

/// <summary>Typed settings checked against the rules: the settings, or the reason they are refused.</summary>
public abstract record SettingsValidation
{
    public sealed record Valid(Settings Settings) : SettingsValidation;

    public sealed record Refused(SettingsProblem Problem) : SettingsValidation;
}