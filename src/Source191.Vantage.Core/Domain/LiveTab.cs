namespace Source191.Vantage.Domain;

/// <summary>Whether a live Vantage tab shows a session, and that tab's colour when one does.</summary>
public abstract record LiveTab
{
    public sealed record None : LiveTab;

    public sealed record Open(TabColour Colour) : LiveTab;
}