namespace Source191.Torni.Application.Views;

/// <summary>What a tab's reducer returns: its new state and the effects to run.</summary>
public sealed record ViewUpdate<TState>(TState State, IReadOnlyList<ViewEffect> Effects)
{
#pragma warning disable CA1000 // Each reducer imports its closed type with using static, so Unchanged and Effect read as they did.
    public static ViewUpdate<TState> Unchanged(TState state) => new(state, []);

    public static ViewUpdate<TState> Effect(TState state, ViewEffect effect) => new(state, [effect]);
#pragma warning restore CA1000
}