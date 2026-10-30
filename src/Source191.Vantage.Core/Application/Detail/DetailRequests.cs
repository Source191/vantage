namespace Source191.Vantage.Application.Detail;

/// <summary>One request for the selected row's detail: its place in the order asked, the row's key, and whether it reads the session's screen.</summary>
public sealed record DetailRequest(int Number, object Key, bool ReadScreen);

/// <summary>
/// The order of one tab's detail requests and which answers the pane shows. Moving the selection asks for the facts at
/// once and, on a tab with a screen, for the screen behind them (it takes a second or two); a refresh asks again, reading
/// the screen every other time. An answer is shown only while its row is still selected: its facts only when no newer
/// facts are shown, its screen only when no newer screen is, and a screen not read keeps the one last shown for the row.
/// Used on the UI thread only.
/// </summary>
public sealed class DetailRequests(bool readsScreen)
{
    private int asked;
    private int refreshes;
    private object? selected;
    private int factsShown;
    private int screenShown;
    private DetailContent? shown;
    private IReadOnlyList<DetailLine>? screen;

    /// <summary>The selection moved to this row, or its tab was shown again.</summary>
    public IReadOnlyList<DetailRequest> Selected(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (Equals(key, selected))
        {
            return [Next(readsScreen)];
        }

        selected = key;
        refreshes = 0;
        factsShown = 0;
        screenShown = 0;
        shown = null;
        screen = null;

        return readsScreen ? [Next(false), Next(true)] : [Next(false)];
    }

    /// <summary>The rows were refreshed with this row selected.</summary>
    public IReadOnlyList<DetailRequest> Refreshed(object key)
    {
        if (!Equals(key, selected))
        {
            return Selected(key);
        }

        refreshes++;

        return [Next(readsScreen && refreshes % 2 == 0)];
    }

    /// <summary>No row is selected: every answer still on its way is dropped.</summary>
    public void Cleared() => selected = null;

    /// <summary>What the pane shows once this answer arrives, or null when the answer is dropped.</summary>
    public DetailContent? Accept(DetailRequest request, DetailContent content)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        bool newerFacts = request.Number > factsShown;
        bool newerScreen = request.ReadScreen && request.Number > screenShown;
        if (!Equals(request.Key, selected) || !(newerFacts || newerScreen))
        {
            return null;
        }

        if (newerFacts)
        {
            factsShown = request.Number;
            shown = content;
        }

        if (newerScreen)
        {
            screenShown = request.Number;
            screen = content.Below;
        }

        return readsScreen ? shown! with { Below = screen ?? Wording.ScreenBeingRead } : shown;
    }

    private DetailRequest Next(bool readScreen) => new(++asked, selected!, readScreen);
}