namespace Source191.Torni.Application;

/// <summary>One item of the key bar: the key as the help names it, its word, the cell it starts at, the tooltip it shows and the key a click on it presses.</summary>
public sealed record KeyBarItem(string Key, string Word, int X, string Tip, Views.Key Press)
{
    /// <summary>The cell after the item's last character.</summary>
    public int End => X + Key.Length + 1 + Word.Length;
}