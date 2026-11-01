namespace Source191.Vantage.Application.Views;

/// <summary>What the Sessions tab's reducer returns, despite the general name: its new state and the effects to run.</summary>
public sealed record ViewUpdate(SessionsViewState State, IReadOnlyList<ViewEffect> Effects);