using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// How one view identifies, filters and orders its rows, and which of them the selection may rest on (a heading may not);
/// a static per view, held by the selection it governs.
/// </summary>
public sealed record ListPolicy<TRow>(
    Func<TRow, string> KeyOf,
    Func<TRow, IReadOnlyList<string>> FilterFields,
    Func<IEnumerable<TRow>, IEnumerable<TRow>> Order,
    StringComparison KeyComparison,
    Func<TRow, bool> Selectable)
{
    /// <summary>A selection with no rows yet, governed by this policy.</summary>
    public ListSelection<TRow> EmptyList() => new(this, [], SelectedKey: null, RowFilter.None, FilterEditing: false, Loaded: false);

    /// <summary>A selection governed by this policy, from a view state's fields; the one place a <see cref="SelectionKey"/> becomes the selection's own key.</summary>
    public ListSelection<TRow> List(IReadOnlyList<TRow> rows, SelectionKey selected, RowFilter filter, bool filterEditing, bool loaded) =>
        new(this, rows, selected is SelectionKey.Row row ? row.Key : null, filter, filterEditing, loaded);
}