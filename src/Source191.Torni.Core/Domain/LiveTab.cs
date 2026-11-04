namespace Source191.Torni.Domain;

/// <summary>Whether a live Torni tab shows a session, and that tab's colour when one does.</summary>
public abstract record LiveTab
{
    public sealed record None : LiveTab;

    public sealed record Open(TabColour Colour) : LiveTab;
}