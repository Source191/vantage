using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

public sealed record SessionsViewState(
    ListSelection<SessionRow> List,
    LoadBanner Banner,
    StatusMessage Message)
    : IListViewState<SessionRow>
{
    private static readonly ListPolicy<SessionRow> Policy = new(
        KeyOf: row => row.Session.SessionId.Value,
        FilterFields: row => [row.Repository, .. FilterText.Of(row.Session.Name), .. FilterText.Of(row.Head)],
        Order: ByAttention,
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>Nothing listed yet: the first listing has not arrived, so the screen shows that it is waiting rather than an empty table.</summary>
    public static readonly SessionsViewState Empty = new(Policy.EmptyList(), new LoadBanner.None(), StatusMessage.None);

    /// <summary>The automated runs the last listing held, for the counts; kept when a listing fails, as the rows are.</summary>
    public AutomatedRuns AutomatedRuns { get; init; } = AutomatedRuns.None;

    public IReadOnlyList<SessionRow> VisibleRows => List.Visible;

    public SessionRow? Selected => List.Selected;

    public TableTitle Title => List.Title("session", row => Wording.Name(row.Session.Name));

    private static IEnumerable<SessionRow> ByAttention(IEnumerable<SessionRow> rows)
    {
        List<SessionRow> matching = [.. rows];
        Sessions ordered = new([.. matching.Select(row => row.Session)]);
        Dictionary<SessionId, SessionRow> bySession = matching.ToDictionary(row => row.Session.SessionId);

        return ordered.OrderedByAttention().Select(session => bySession[session.SessionId]);
    }
}