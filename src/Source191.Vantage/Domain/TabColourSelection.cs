namespace Source191.Vantage.Domain;

public sealed record ColourChoice(TabColour Colour, bool Reused);

public sealed class TabColourSelection(IReadOnlyList<TabColour> palette)
{
    public ColourChoice Choose(IReadOnlyList<TabColour> inUse)
    {
        TabColour? free = palette.FirstOrDefault(colour => !inUse.Contains(colour));
        if (free is not null)
        {
            return new ColourChoice(free, Reused: false);
        }

        TabColour leastUsed = palette
            .OrderBy(colour => inUse.Count(used => used == colour))
            .First();

        return new ColourChoice(leastUsed, Reused: true);
    }
}