namespace Source191.Vantage.Application.Views;

/// <summary>The keys that scroll a table too wide for the terminal: &lt; and &gt; (Shift+arrows did not reach Vantage in Windows Terminal); Left and Right are the tab keys.</summary>
public static class ScrollKeys
{
    /// <summary>The columns to scroll by, -1 or 1; false for a key that does not scroll.</summary>
    public static bool TryFor(Key key, out int step)
    {
        ArgumentNullException.ThrowIfNull(key);
        step = key switch
        {
            { Name: KeyName.Character, Character: '<' } => -1,
            { Name: KeyName.Character, Character: '>' } => 1,
            _ => 0,
        };

        return step != 0;
    }
}