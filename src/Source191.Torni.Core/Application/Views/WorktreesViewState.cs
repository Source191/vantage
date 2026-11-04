namespace Source191.Torni.Application.Views;

public sealed record WorktreesViewState(
    ListSelection<WorktreeRow> List,
    LoadBanner Banner,
    StatusMessage Message)
    : IListViewState<WorktreeRow>
{
    private static readonly ListPolicy<WorktreeRow> Policy = new(
        KeyOf: row => row.Path.Key,
        FilterFields: row => [row.RepositoryName, row.FolderName, .. FilterText.Of(row.Head), row.Health.Kind.ToString()],
        Order: rows => rows.OrderBy(row => row.Repository.Key, StringComparer.Ordinal).ThenBy(row => row.FolderName, StringComparer.OrdinalIgnoreCase),
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>Nothing scanned yet: the first scan has not arrived, so the screen shows that it is scanning rather than an empty table.</summary>
    public static readonly WorktreesViewState Empty = new(Policy.EmptyList(), new LoadBanner.None(), StatusMessage.None);

    public IReadOnlyList<WorktreeRow> VisibleRows => List.Visible;

    public WorktreeRow? Selected => List.Selected;

    public TableTitle Title => List.Title("worktree", row => row.FolderName);
}