namespace Source191.Vantage.Application.Detail;

/// <summary>One request for the selected row's detail: its place in the order asked, and the row's key.</summary>
public sealed record DetailRequest(int Number, object Key);