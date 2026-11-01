namespace Source191.Vantage.Application.Detail;

/// <summary>
/// The order of one tab's detail requests and which answers the pane shows: moving the selection or refreshing the rows
/// asks again, and an answer is shown only while its row is still selected and no newer answer has been shown. Used on
/// the UI thread only.
/// </summary>
public sealed class DetailRequests
{
    /// <summary>The selection while no row is selected: no row's key equals it.</summary>
    private static readonly object NoRow = new();

    private int asked;
    private int shown;
    private object selected = NoRow;

    /// <summary>The selection moved to this row, the rows were refreshed, or its tab was shown again.</summary>
    public DetailRequest Ask(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!Equals(key, selected))
        {
            selected = key;
            shown = 0;
        }

        return new DetailRequest(++asked, key);
    }

    /// <summary>No row is selected: every answer still on its way is dropped.</summary>
    public void Cleared() => selected = NoRow;

    /// <summary>What the pane does once this answer arrives: shows it, or drops it.</summary>
    public DetailAnswer Accept(DetailRequest request, DetailContent content)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        if (!Equals(request.Key, selected) || request.Number <= shown)
        {
            return new DetailAnswer.Dropped();
        }

        shown = request.Number;

        return new DetailAnswer.Show(content);
    }
}