namespace Source191.Vantage.Application.Views;

/// <summary>Which row a view state has selected, by the row's key so it survives a refresh, or none in an empty list.</summary>
public abstract record SelectionKey
{
    public sealed record None : SelectionKey;

    public sealed record Row(string Key) : SelectionKey;
}