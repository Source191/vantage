using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Opens a solution file with the application Windows associates with its type; a type with none is an outcome in Vantage's words.</summary>
public sealed class OpenSolutionHandler(IDesktop desktop) : IEffectHandler<OpenSolution>
{
    public async Task<Outcome> HandleAsync(OpenSolution effect, CancellationToken cancellationToken)
    {
        try
        {
            await desktop.OpenAsync(effect.File, cancellationToken);
        }
        catch (NoApplicationException none)
        {
            return new OpenSolutionOutcome.NoApplication(none.Extension);
        }

        return new OpenSolutionOutcome.Opened(Path.GetFileName(effect.File));
    }
}