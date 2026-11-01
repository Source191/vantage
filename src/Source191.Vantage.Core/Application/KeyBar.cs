namespace Source191.Vantage.Application;

/// <summary>The two rows of the key bar; <see cref="Width"/> is the cells the wider row reaches.</summary>
public sealed record KeyBar(IReadOnlyList<KeyBarItem> First, IReadOnlyList<KeyBarItem> Second)
{
    public int Width => First.Concat(Second).Select(item => item.End).DefaultIfEmpty(0).Max();
}