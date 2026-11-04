using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

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
}