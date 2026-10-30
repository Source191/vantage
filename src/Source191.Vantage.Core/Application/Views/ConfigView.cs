using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record ConfigRowsLoaded(IReadOnlyList<ConfigRow> Rows) : ViewInput;

/// <summary>Edit one setting in a dialog of its own; the dialog settles on <see cref="SaveSettings"/>.</summary>
public sealed record EditSetting(ConfigRow Row) : ViewEffect, IDialogEffect;

/// <summary>Add a repository root in the root dialog, opened empty.</summary>
public sealed record AddRepositoryRoot : ViewEffect, IDialogEffect;

public sealed record RemoveRepositoryRoot(int Index) : ViewEffect;

public sealed record ToggleUpdateCheck : ViewEffect;

public sealed record ConfigViewUpdate(ConfigViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record ConfigViewState(
    IReadOnlyList<ConfigRow> Rows,
    string? SelectedKey,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    Footer Footer,
    bool Loaded = false)
    : IListViewState<ConfigRow>
{
    public static readonly ConfigViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Footer: Footer.None);

    private static readonly ListPolicy<ConfigRow> Policy = new(
        KeyOf: row => row.Key,
        FilterFields: row => [Wording.ConfigSetting(row), ConfigView.PathOf(row)],
        Order: rows => rows,
        KeyComparison: StringComparison.Ordinal);

    public ListSelection<ConfigRow> List => new(Policy, Rows, SelectedKey, Filter, FilterEditing, Loaded);

    public ConfigRow? Selected => List.Selected;

    public Footer Shown => Footer is Footer.Blank ? new Footer.ConfigHint() : Footer;

    public TableTitle Title => List.Title("setting", Wording.ConfigSetting);

    public ConfigViewState With(ListSelection<ConfigRow> list) => this with { Rows = list.Rows, SelectedKey = list.SelectedKey, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };
}

/// <summary>The Config tab as a pure function of state and input, as <see cref="WorktreesView"/> is for its tab.</summary>
public static class ConfigView
{
    public static ConfigViewUpdate Reduce(ConfigViewState state, ViewInput input) => new(state, []);

    /// <summary>The row's value when it is a path (a root, the worktree root, a setup file), else null.</summary>
    public static string? PathOf(ConfigRow row) => null;
}
