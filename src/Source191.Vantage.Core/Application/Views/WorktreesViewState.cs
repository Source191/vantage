using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record WorktreesViewState(
    IReadOnlyList<WorktreeRow> Rows,
    SelectionKey SelectedPath,
    RowFilter Filter,
    bool FilterEditing,
    LoadBanner Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<WorktreeRow>
{
    /// <summary>Nothing scanned yet: the first scan has not arrived, so the screen shows that it is scanning rather than an empty table.</summary>
    public static readonly WorktreesViewState Empty = new([], new SelectionKey.None(), new RowFilter(string.Empty), FilterEditing: false, Banner: new LoadBanner.None(), Message: StatusMessage.None);

    private static readonly ListPolicy<WorktreeRow> Policy = new(
        KeyOf: row => row.Path.Key,
        FilterFields: row => [row.RepositoryName, row.FolderName, .. FilterText.Of(row.Head), row.Health.Kind.ToString()],
        Order: rows => rows.OrderBy(row => row.Repository.Key, StringComparer.Ordinal).ThenBy(row => row.FolderName, StringComparer.OrdinalIgnoreCase),
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<WorktreeRow> List => Policy.List(Rows, SelectedPath, Filter, FilterEditing, Loaded);

    public IReadOnlyList<WorktreeRow> VisibleRows => List.Visible;

    public WorktreeRow? Selected => List.Selected;

    /// <summary>This state with the list component's fields taken from <paramref name="list"/>.</summary>
    public WorktreesViewState With(ListSelection<WorktreeRow> list) => this with { Rows = list.Rows, SelectedPath = list.Selection, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };

    /// <summary>The last message only: the selected row's health is the detail pane's.</summary>
    public StatusMessage Shown => Message;

    public TableTitle Title => List.Title("worktree", row => row.FolderName);
}