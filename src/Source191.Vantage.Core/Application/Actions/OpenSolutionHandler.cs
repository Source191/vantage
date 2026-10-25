using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Opens a solution file with the application Windows associates with its type.</summary>
public sealed class OpenSolutionHandler(IDesktop desktop) : IEffectHandler<OpenSolution>
{
    public async Task<Outcome> HandleAsync(OpenSolution effect, CancellationToken cancellationToken)
    {
        await desktop.OpenAsync(effect.File, cancellationToken);

        return new OpenedSolution(Path.GetFileName(effect.File));
    }
}