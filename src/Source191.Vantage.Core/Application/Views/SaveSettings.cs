using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>What the settings dialog settles on: write config.json and use these settings from now on.</summary>
public sealed record SaveSettings(Settings Settings) : ViewEffect;