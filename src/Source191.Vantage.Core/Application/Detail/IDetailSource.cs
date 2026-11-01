namespace Source191.Vantage.Application.Detail;

/// <summary>How a tab's detail pane reads its selected row: the lines its facts can take, the row's key across refreshes, and the pane's content.</summary>
public interface IDetailSource<in TRow>
{
    /// <summary>The most lines under the divider this tab's detail can take, so the pane is always that tall.</summary>
    int Rows { get; }

    object KeyOf(TRow row);

    Task<DetailContent> ReadAsync(TRow row, CancellationToken cancellationToken);
}