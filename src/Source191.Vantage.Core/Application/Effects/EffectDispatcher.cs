using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Effects;

/// <summary>
/// Routes a background effect to the one handler registered for its type. The composition root registers every
/// handler; a screen only asks for an effect to be dispatched, so adding a key never edits a screen.
/// </summary>
public sealed class EffectDispatcher
{
    private readonly Dictionary<Type, Func<ViewEffect, CancellationToken, Task<Outcome>>> handlers = [];

    public EffectDispatcher Register<TEffect>(IEffectHandler<TEffect> handler)
        where TEffect : ViewEffect
    {
        handlers[typeof(TEffect)] = (effect, cancellationToken) => handler.HandleAsync((TEffect)effect, cancellationToken);

        return this;
    }

    /// <summary>Whether an effect of this type has a handler; the registration completeness test asks this.</summary>
    public bool Handles(Type effectType) => handlers.ContainsKey(effectType);

    public Task<Outcome> DispatchAsync(ViewEffect effect, CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(effect.GetType(), out Func<ViewEffect, CancellationToken, Task<Outcome>>? handler))
        {
            throw new InvalidOperationException($"No handler is registered for {effect.GetType().Name}.");
        }

        return handler(effect, cancellationToken);
    }
}