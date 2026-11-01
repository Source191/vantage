namespace Source191.Vantage.Application.Views;

public sealed record ViewUpdate(SessionsViewState State, IReadOnlyList<ViewEffect> Effects);