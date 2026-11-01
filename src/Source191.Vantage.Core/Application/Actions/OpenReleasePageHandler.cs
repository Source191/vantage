using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Opens a release's GitHub page (<see cref="ReleasePage"/>) in the default browser.</summary>
public sealed class OpenReleasePageHandler(IDesktop desktop) : IEffectHandler<OpenReleasePage>
{
    public async Task<Outcome> HandleAsync(OpenReleasePage effect, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(effect);
        try
        {
            await desktop.OpenAsync(ReleasePage.For(effect.Version).AbsoluteUri, cancellationToken);
        }
        catch (InvalidOperationException failed)
        {
            return new OpenReleasePageOutcome.NotOpened(effect.Version.ToString(), failed.Message);
        }

        return new OpenReleasePageOutcome.Opened(effect.Version.ToString());
    }
}