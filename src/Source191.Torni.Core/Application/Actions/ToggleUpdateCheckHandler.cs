using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>Turns the update check the other way and saves the settings.</summary>
public sealed class ToggleUpdateCheckHandler(CurrentSettings settings) : IEffectHandler<ToggleUpdateCheck>
{
    public async Task<Outcome> HandleAsync(ToggleUpdateCheck effect, CancellationToken cancellationToken)
    {
        Settings toggled = settings.Value.WithUpdateCheckToggled();
        await settings.SaveAsync(toggled);

        return new ToggleUpdateCheckOutcome(toggled.CheckForUpdates);
    }
}