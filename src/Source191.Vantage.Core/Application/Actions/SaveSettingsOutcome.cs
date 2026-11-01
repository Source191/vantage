using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>The settings were written and apply from the next scan, wizard, dialog and Config tab read on.</summary>
public sealed record SaveSettingsOutcome : Outcome;