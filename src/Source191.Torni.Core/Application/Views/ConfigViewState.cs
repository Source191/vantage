namespace Source191.Torni.Application.Views;

public sealed record ConfigViewState(
    ListSelection<ConfigRow> List,
    LoadBanner Banner,
    StatusMessage Message)
    : IListViewState<ConfigRow>
{
    private static readonly ListPolicy<ConfigRow> Policy = new(
        KeyOf: row => row.Key,
        FilterFields: FilterFieldsOf,
        Order: rows => rows,
        KeyComparison: StringComparison.Ordinal,
        Selectable: row => row is not ConfigRow.Heading);

    public static readonly ConfigViewState Empty = new(Policy.EmptyList(), new LoadBanner.None(), StatusMessage.None);

    public ConfigRow? Selected => List.Selected;

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

}