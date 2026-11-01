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

/// <summary>
/// The part of a table view that every table view has: the rows, which one is selected (by key, so it survives a
/// refresh that reorders rows), the filter being typed, and whether the first load has arrived. Every operation is a
/// pure function returning the next selection; the views compose it and add what is theirs.
/// </summary>
public sealed record ListSelection<TRow>(
    ListPolicy<TRow> Policy,
    IReadOnlyList<TRow> Rows,
    string? SelectedKey,
    RowFilter Filter,
    bool FilterEditing,
    bool Loaded)
{
    public IReadOnlyList<TRow> Visible => [.. Policy.Order(Rows.Where(row => Filter.Matches(Policy.FilterFields(row))))];

    public TRow? Selected => Visible.FirstOrDefault(IsSelected);

    /// <summary>The visible rows the selection may rest on, in table order.</summary>
    private IReadOnlyList<TRow> Choosable => [.. Visible.Where(Policy.Selectable)];

    /// <summary>The selected key as a view state holds it.</summary>
    public SelectionKey Selection => SelectedKey is string key ? new SelectionKey.Row(key) : new SelectionKey.None();

    /// <summary>What the table's title says: how many rows show of how many, the filter if one is set, and the selected row's label.</summary>
    public TableTitle Title(string noun, Func<TRow, string> labelOf)
    {
        TRow? current = Selected;

        return new TableTitle(noun, Choosable.Count, Rows.Count(Policy.Selectable), Filter.Text, current is null ? null : labelOf(current));
    }
    /// <summary>
    /// New rows keep the selected key when it is still visible, otherwise the selectable row nearest its position; the
    /// first load selects the first selectable row.
    /// </summary>
    public ListSelection<TRow> Load(IReadOnlyList<TRow> rows)
    {
        int previousIndex = IndexOfSelected();
        ListSelection<TRow> loaded = this with { Rows = rows, Loaded = true };
        if (loaded.Selected is not null)
        {
            return loaded;
        }

        IReadOnlyList<TRow> visible = loaded.Visible;
        List<int> choosable = [.. Enumerable.Range(0, visible.Count).Where(index => Policy.Selectable(visible[index]))];
        if (choosable.Count == 0)
        {
            return loaded with { SelectedKey = null };
        }

        int nearest = choosable.MinBy(index => Math.Abs(index - previousIndex));

        return loaded with { SelectedKey = Policy.KeyOf(visible[nearest]) };
    }

    /// <summary>Moves the selection by this many selectable rows, stepping over the rest and staying put at either end.</summary>
    public ListSelection<TRow> Move(int delta)
    {
        IReadOnlyList<TRow> choosable = Choosable;
        if (choosable.Count == 0)
        {
            return this;
        }

        int position = Math.Max(choosable.ToList().FindIndex(IsSelected), 0);
        int index = Math.Clamp(position + delta, 0, choosable.Count - 1);

        return this with { SelectedKey = Policy.KeyOf(choosable[index]) };
    }

    public ListSelection<TRow> StartFilter() => this with { FilterEditing = true, Filter = RowFilter.None };

    public ListSelection<TRow> ClearFilter() => (this with { FilterEditing = false, Filter = RowFilter.None }).Reselect();

    /// <summary>A key typed while the filter is being edited: Enter keeps it, Escape clears it, the rest edit the text and keep a row selected.</summary>
    public ListSelection<TRow> EditFilter(Key key)
    {
        return key.Name switch
        {
            KeyName.Enter => this with { FilterEditing = false },
            KeyName.Escape => ClearFilter(),
            KeyName.Backspace => (this with { Filter = new RowFilter(Filter.Text[..Math.Max(0, Filter.Text.Length - 1)]) }).Reselect(),
            KeyName.Character => (this with { Filter = new RowFilter(Filter.Text + key.Character) }).Reselect(),
            KeyName.Up => this,
            KeyName.Down => this,
            KeyName.Left => this,
            KeyName.Right => this,
        };
    }

    /// <summary>Selects the selectable row with this key when there is one; a filter that would hide it is cleared so it shows.</summary>
    public ListSelection<TRow> Select(string key)
    {
        if (!Rows.Any(row => Policy.Selectable(row) && string.Equals(Policy.KeyOf(row), key, Policy.KeyComparison)))
        {
            return this;
        }

        ListSelection<TRow> selected = this with { SelectedKey = key };

        return selected.Selected is null
            ? selected with { Filter = RowFilter.None, FilterEditing = false }
            : selected;
    }

    private ListSelection<TRow> Reselect() => Selected is not null ? this : Load(Rows);

    private int IndexOfSelected() => Math.Max(Visible.ToList().FindIndex(IsSelected), 0);

    private bool IsSelected(TRow row) => SelectedKey is not null && string.Equals(Policy.KeyOf(row), SelectedKey, Policy.KeyComparison);
}
/// <summary>The fact behind a table's title: <see cref="Noun"/> is singular (session), <see cref="Filter"/> empty when none is set and <see cref="Selected"/> null when no row is.</summary>
public sealed record TableTitle(string Noun, int Shown, int Total, string Filter, string? Selected);