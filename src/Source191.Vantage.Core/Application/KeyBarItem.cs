namespace Source191.Vantage.Application;

/// <summary>One item of the key bar: the key as the help names it, its word, the cell it starts at and the tooltip it shows.</summary>
public sealed record KeyBarItem(string Key, string Word, int X, string Tip)
{
    /// <summary>The cell after the item's last character.</summary>
    public int End => X + Key.Length + 1 + Word.Length;
}