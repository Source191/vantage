using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Actions;

/// <summary>Writes the settings the dialog settled on to config.json and makes them the current ones.</summary>
public sealed class SaveSettingsHandler(CurrentSettings settings) : IEffectHandler<SaveSettings>
{
    public async Task<Outcome> HandleAsync(SaveSettings effect, CancellationToken cancellationToken)
    {
        await settings.SaveAsync(effect.Settings);

        return new SaveSettingsOutcome();
    }
}

/// <summary>The settings were written and apply from the next scan, wizard and header on.</summary>
public sealed record SaveSettingsOutcome : Outcome;
