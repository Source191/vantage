using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record ConfigViewState(
    IReadOnlyList<ConfigRow> Rows,
    SelectionKey SelectedKey,
    RowFilter Filter,
    bool FilterEditing,
    LoadBanner Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<ConfigRow>
{
    public static readonly ConfigViewState Empty = new([], new SelectionKey.None(), new RowFilter(string.Empty), FilterEditing: false, Banner: new LoadBanner.None(), Message: StatusMessage.None);

    private static readonly ListPolicy<ConfigRow> Policy = new(
        KeyOf: row => row.Key,
        FilterFields: FilterFieldsOf,
        Order: rows => rows,
        KeyComparison: StringComparison.Ordinal,
        Selectable: row => row is not ConfigRow.Heading);

    public ListSelection<ConfigRow> List => Policy.List(Rows, SelectedKey, Filter, FilterEditing, Loaded);

    public ConfigRow? Selected => List.Selected;

    /// <summary>The last message only: what the selected row is for is its tooltip.</summary>
    public StatusMessage Shown => Message;

    public TableTitle Title => List.Title("row", Wording.ConfigSetting);

    /// <summary>A row's setting and its path when it has one; the heading has none, so any filter hides it.</summary>
    private static IReadOnlyList<string> FilterFieldsOf(ConfigRow row)
    {
        if (row is ConfigRow.Heading)
        {
            return [];
        }

        return [Wording.ConfigSetting(row), ConfigView.TryPathOf(row, out string? path) ? path : string.Empty];
    }

    public ConfigViewState With(ListSelection<ConfigRow> list) => this with { Rows = list.Rows, SelectedKey = list.Selection, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };
}