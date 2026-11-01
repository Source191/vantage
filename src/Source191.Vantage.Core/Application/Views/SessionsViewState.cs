using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record SessionsViewState(
    IReadOnlyList<SessionRow> Rows,
    SelectionKey SelectedSessionId,
    RowFilter Filter,
    bool FilterEditing,
    LoadBanner Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<SessionRow>
{
    /// <summary>Nothing listed yet: the first listing has not arrived, so the screen shows that it is waiting rather than an empty table.</summary>
    public static readonly SessionsViewState Empty = new([], new SelectionKey.None(), new RowFilter(string.Empty), FilterEditing: false, Banner: new LoadBanner.None(), Message: StatusMessage.None);

    /// <summary>The automated runs the last listing held, for the counts; kept when a listing fails, as the rows are.</summary>
    public AutomatedRuns AutomatedRuns { get; init; } = AutomatedRuns.None;

    private static readonly ListPolicy<SessionRow> Policy = new(
        KeyOf: row => row.Session.SessionId.Value,
        FilterFields: row => [row.Session.Name, row.Repository, .. FilterText.Of(row.Head)],
        Order: ByAttention,
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<SessionRow> List => Policy.List(Rows, SelectedSessionId, Filter, FilterEditing, Loaded);

    public IReadOnlyList<SessionRow> VisibleRows => List.Visible;

    public SessionRow? Selected => List.Selected;


    public TableTitle Title => List.Title("session", row => row.Session.Name);

    /// <summary>The status message to show: the last message; <see cref="StatusMessage.Blank"/> shows no status line.</summary>
    public StatusMessage Shown => Message;

    /// <summary>This state with the list component's fields taken from <paramref name="list"/>.</summary>
    public SessionsViewState With(ListSelection<SessionRow> list) => this with { Rows = list.Rows, SelectedSessionId = list.Selection, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };

    private static IEnumerable<SessionRow> ByAttention(IEnumerable<SessionRow> rows)
    {
        List<SessionRow> matching = [.. rows];
        Sessions ordered = new([.. matching.Select(row => row.Session)]);
        Dictionary<SessionId, SessionRow> bySession = matching.ToDictionary(row => row.Session.SessionId);

        return ordered.OrderedByAttention().Select(session => bySession[session.SessionId]);
    }
}