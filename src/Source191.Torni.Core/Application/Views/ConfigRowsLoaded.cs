namespace Source191.Torni.Application.Views;

public sealed record ConfigRowsLoaded(IReadOnlyList<ConfigRow> Rows) : ViewInput;