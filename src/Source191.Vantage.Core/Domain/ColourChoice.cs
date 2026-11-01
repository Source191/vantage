namespace Source191.Vantage.Domain;

/// <summary>
/// The colour a new tab gets. Reused is true when every palette colour was already in use and the least-used one was
/// given out again.
/// </summary>
public sealed record ColourChoice(TabColour Colour, bool Reused);