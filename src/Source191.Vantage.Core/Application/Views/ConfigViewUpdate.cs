namespace Source191.Vantage.Application.Views;

public sealed record ConfigViewUpdate(ConfigViewState State, IReadOnlyList<ViewEffect> Effects);