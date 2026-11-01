using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record ConfigRowsLoaded(IReadOnlyList<ConfigRow> Rows) : ViewInput;

/// <summary>Edit one setting in a dialog of its own; the dialog settles on <see cref="SaveSettings"/>.</summary>
public sealed record EditSetting(SettingsScope Scope) : ViewEffect, IDialogEffect;

public sealed record ToggleUpdateCheck : ViewEffect;

public sealed record ConfigViewUpdate(ConfigViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record ConfigViewState(
    IReadOnlyList<ConfigRow> Rows,
    string? SelectedKey,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<ConfigRow>
{
    public static readonly ConfigViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Message: StatusMessage.None);

    private static readonly ListPolicy<ConfigRow> Policy = new(
        KeyOf: row => row.Key,
        FilterFields: row => [Wording.ConfigSetting(row), ConfigView.PathOf(row)],
        Order: rows => rows,
        KeyComparison: StringComparison.Ordinal);

    public ListSelection<ConfigRow> List => new(Policy, Rows, SelectedKey, Filter, FilterEditing, Loaded);

    public ConfigRow? Selected => List.Selected;

    /// <summary>The last message, else what the selected row is for.</summary>
    public StatusMessage Shown => (Message, Selected) switch
    {
        (StatusMessage.Blank, ConfigRow row) => new StatusMessage.SettingHint(row),
        _ => Message,
    };

    public TableTitle Title => List.Title("row", Wording.ConfigSetting);

    public ConfigViewState With(ListSelection<ConfigRow> list) => this with { Rows = list.Rows, SelectedKey = list.SelectedKey, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };
}

/// <summary>The Config tab as a pure function of state and input, as <see cref="WorktreesView"/> is for its tab.</summary>
public static class ConfigView
{
    public static ConfigViewUpdate Reduce(ConfigViewState state, ViewInput input)
    {
        ArgumentNullException.ThrowIfNull(state);

        return input switch
        {
            ConfigRowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = null }),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            UpdateTabOpened => Effect(state, new Quit()),
            KeyPressed pressed when state.FilterEditing => Unchanged(state.With(state.List.EditFilter(pressed.Key)) with { Message = StatusMessage.None }),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    /// <summary>The row's value when it is a path (a root, the worktree root, a setup file), else null.</summary>
    public static string? PathOf(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.RepositoryRoot root => root.Path.Value,
            ConfigRow.WorktreeRoot worktreeRoot => worktreeRoot.Path.Value,
            ConfigRow.Setup { State: SetupState.At at } => at.Path,
            _ => null,
        };
    }

    private static ConfigViewUpdate Act(ConfigViewState state, Key key)
    {
        return key switch
        {
            // Moving on or filtering clears the last message, so the status line goes back to what the selected row is for.
            { Name: KeyName.Up } => Unchanged(state.With(state.List.Move(-1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Down } => Unchanged(state.With(state.List.Move(+1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Escape } when state.Filter.Text.Length > 0 => Unchanged(state.With(state.List.ClearFilter())),
            { Name: KeyName.Escape } => Effect(state, new SelectTab(ConsoleTab.Sessions)),
            { Name: KeyName.Enter } => RowAction(state, Enter),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter()) with { Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'c' } => RowAction(state, row => WithPath(row, path => new CopyFolderPath(FolderPath.From(path)))),
            { Name: KeyName.Character, Character: 'e' } => RowAction(state, row => WithPath(row, path => new OpenFolderInEditor(FolderOf(row, path)))),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            { Name: KeyName.Character, Character: 'U' } => Effect(state, new UpdateVantage()),
            { Name: KeyName.Character, Character: '!' } => Effect(state, new ShowProblemReport()),
            _ when TabKeys.For(key) is ViewEffect tab => Effect(state, tab),
            _ => Unchanged(state),
        };
    }

    /// <summary>Either an effect to run or a status message explaining why nothing happens.</summary>
    private static ConfigViewUpdate RowAction(ConfigViewState state, Func<ConfigRow, (ViewEffect? Effect, StatusMessage Message)> decide)
    {
        if (state.Selected is not ConfigRow row)
        {
            return Unchanged(state with { Message = new StatusMessage.NoSettingSelected() });
        }

        (ViewEffect? effect, StatusMessage message) = decide(row);

        return effect is null
            ? Unchanged(state with { Message = message })
            : Effect(state with { Message = message }, effect);
    }

    private static (ViewEffect?, StatusMessage) Enter(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.CheckForUpdates => (new ToggleUpdateCheck(), StatusMessage.None),
            ConfigRow.Setup setup => (null, new StatusMessage.SetupAdvice(setup.Item)),
            _ => (new EditSetting(SettingsScope.For(row)), StatusMessage.None),
        };
    }

    private static (ViewEffect?, StatusMessage) WithPath(ConfigRow row, Func<string, ViewEffect> effect) => PathOf(row) is string path ? (effect(path), StatusMessage.None) : (null, new StatusMessage.NoPathHere());

    /// <summary>A setup row's path is a file, so the folder holding it is the one to open; a root is a folder already.</summary>
    private static FolderPath FolderOf(ConfigRow row, string path) => row is ConfigRow.Setup && FolderPath.From(path).Parent is FolderPath parent ? parent : FolderPath.From(path);

    private static ConfigViewUpdate Unchanged(ConfigViewState state) => new(state, []);

    private static ConfigViewUpdate Effect(ConfigViewState state, ViewEffect effect) => new(state, [effect]);
}