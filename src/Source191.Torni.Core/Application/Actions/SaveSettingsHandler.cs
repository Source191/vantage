using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;

namespace Source191.Torni.Application.Actions;

/// <summary>Writes the settings the dialog settled on to config.json and makes them the current ones.</summary>
public sealed class SaveSettingsHandler(CurrentSettings settings) : IEffectHandler<SaveSettings>
{
    public async Task<Outcome> HandleAsync(SaveSettings effect, CancellationToken cancellationToken)
    {
        await settings.SaveAsync(effect.Settings);

        return new SaveSettingsOutcome();
    }
}