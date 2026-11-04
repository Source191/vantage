using Source191.Torni.Domain;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Source191.Torni.Application.Views.ViewUpdate<Source191.Torni.Application.Views.ConfigViewState>;

namespace Source191.Torni.Application.Views;

/// <summary>The Config tab as a pure function of state and input, as <see cref="WorktreesView"/> is for its tab.</summary>
public static class ConfigView
{
    public static ViewUpdate<ConfigViewState> Reduce(ConfigViewState state, ViewInput input)
    {
        return input switch
        {
            ConfigRowsLoaded loaded => Unchanged(state with { List = state.List.Load(loaded.Rows), Banner = new LoadBanner.None() }),
            LoadFailed failed => Unchanged(state with { Banner = new LoadBanner.Failed(failed.Message) }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            UpdateTabOpened => Effect(state, new Quit()),
            KeyPressed pressed when state.List.FilterEditing => Unchanged(state with { List = state.List.EditFilter(pressed.Key), Message = StatusMessage.None }),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    /// <summary>Whether the row's value is a path (a root, the worktree root, a setup file), and the path when it is.</summary>
    public static bool TryPathOf(ConfigRow row, [MaybeNullWhen(false)] out string path)
    {
        path = row switch
        {
            ConfigRow.RepositoryRoot root => root.Path.Value,
            ConfigRow.WorktreeRoot worktreeRoot => worktreeRoot.Path.Value,
            ConfigRow.Setup { State: SetupState.At at } => at.Path,
            _ => null,
        };

        return path is not null;
    }

    private static ViewUpdate<ConfigViewState> Act(ConfigViewState state, Key key)
    {
        return key switch
        {
            // Moving on or filtering clears the last message; what the selected row is for is only its tooltip.
            { Name: KeyName.Up } => Unchanged(state with { List = state.List.Move(-1), Message = StatusMessage.None }),
            { Name: KeyName.Down } => Unchanged(state with { List = state.List.Move(+1), Message = StatusMessage.None }),
            { Name: KeyName.Escape } when state.List.Filter.Text.Length > 0 => Unchanged(state with { List = state.List.ClearFilter(), Message = StatusMessage.None }),
            { Name: KeyName.Escape } => Effect(state, new SelectTab(ConsoleTab.Sessions)),
            { Name: KeyName.Enter } => RowAction(state, Enter),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state with { List = state.List.StartFilter(), Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'c' } => RowAction(state, row => WithPath(row, path => new CopyFolderPath(FolderPath.From(path)))),
            { Name: KeyName.Character, Character: 'f' } => RowAction(state, row => WithPath(row, path => new OpenFolderInEditor(FolderOf(row, path)))),
            _ when SharedKeys.TryFor(key, out ViewEffect? shared) => Effect(state, shared),
            _ when TabKeys.TryFor(key, out ViewEffect? tab) => Effect(state, tab),
            _ => Unchanged(state),
        };
    }

    /// <summary>Either an effect to run or a status message explaining why nothing happens.</summary>
    private static ViewUpdate<ConfigViewState> RowAction(ConfigViewState state, Func<ConfigRow, RowDecision> decide)
    {
        if (state.Selected is not ConfigRow row)
        {
            return Unchanged(state with { Message = new StatusMessage.NoSettingSelected() });
        }

        return decide(row) switch
        {
            RowDecision.Act act => Effect(state with { Message = StatusMessage.None }, act.Effect),
            RowDecision.Explain explain => Unchanged(state with { Message = explain.Message }),
            _ => throw new UnreachableException(),
        };
    }

    private static RowDecision Enter(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.CheckForUpdates => new RowDecision.Act(new ToggleUpdateCheck()),
            ConfigRow.Setup setup => new RowDecision.Explain(new StatusMessage.SetupAdvice(setup.Item)),
            _ => new RowDecision.Act(new EditSetting(SettingsScope.For(row))),
        };
    }

    private static RowDecision WithPath(ConfigRow row, Func<string, ViewEffect> effect) => TryPathOf(row, out string? path) ? new RowDecision.Act(effect(path)) : new RowDecision.Explain(new StatusMessage.NoPathHere());

    /// <summary>A setup row's path is a file, so the folder holding it is the one to open; a root is a folder already.</summary>
    private static FolderPath FolderOf(ConfigRow row, string path)
    {
        FolderPath folder = FolderPath.From(path);

        return row is ConfigRow.Setup && folder.Parent is FolderPath parent ? parent : folder;
    }
}