namespace Source191.Torni.Tui;

/// <summary>How a tab is kept fresh: never on a timer, or every interval.</summary>
internal abstract record TabRefresh
{
    private TabRefresh()
    {
    }

    /// <summary>No timer: the tab reads its rows when shown and after an action.</summary>
    public sealed record Never : TabRefresh;

    public sealed record Every(TimeSpan Interval) : TabRefresh;
}