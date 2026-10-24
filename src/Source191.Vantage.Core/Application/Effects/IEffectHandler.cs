using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Effects;

/// <summary>
/// Carries out one kind of background effect and says what happened as an <see cref="Outcome"/>; Wording gives the words. One class per
/// effect, with only the ports that effect needs; the screen routes to it through the <see cref="EffectDispatcher"/>.
/// </summary>
public interface IEffectHandler<in TEffect>
    where TEffect : ViewEffect
{
    Task<Outcome> HandleAsync(TEffect effect, CancellationToken cancellationToken);
}