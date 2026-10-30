using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>Removes one repository root from the settings and saves them, never the last.</summary>
public sealed class RemoveRepositoryRootHandler(CurrentSettings settings) : IEffectHandler<RemoveRepositoryRoot>
{
    public async Task<Outcome> HandleAsync(RemoveRepositoryRoot effect, CancellationToken cancellationToken)
    {
        Settings current = settings.Value;
        if (current.WithRootRemoved(effect.Index) is not SettingsValidation.Valid valid)
        {
            return new RemoveRepositoryRootOutcome.OnlyRoot();
        }

        await settings.SaveAsync(valid.Settings);

        return new RemoveRepositoryRootOutcome.Removed(current.RepositoryRoots[effect.Index]);
    }
}