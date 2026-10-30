namespace Source191.Vantage.Application.Detail;

/// <summary>One request for the selected row's detail: its place in the order asked, and the row's key.</summary>
public sealed record DetailRequest(int Number, object Key);

/// <summary>
/// The order of one tab's detail requests and which answers the pane shows: moving the selection or refreshing the rows
/// asks again, and an answer is shown only while its row is still selected and no newer answer has been shown. Used on
/// the UI thread only.
/// </summary>
public sealed class DetailRequests
{
    private int asked;
    private int shown;
    private object? selected;

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
    public void Cleared() => selected = null;

    /// <summary>What the pane shows once this answer arrives, or null when the answer is dropped.</summary>
    public DetailContent? Accept(DetailRequest request, DetailContent content)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        if (!Equals(request.Key, selected) || request.Number <= shown)
        {
            return null;
        }

        shown = request.Number;

        return content;
    }
}