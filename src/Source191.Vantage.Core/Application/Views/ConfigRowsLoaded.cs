namespace Source191.Vantage.Application.Views;

public sealed record ConfigRowsLoaded(IReadOnlyList<ConfigRow> Rows) : ViewInput;