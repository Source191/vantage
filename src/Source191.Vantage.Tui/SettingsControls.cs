using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Diagnostics;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// A setting's dialog's controls and where they sit: only the ones its <see cref="SettingsScope"/> edits, a caption
/// above them and the problem below. <see cref="SettingsDialog"/> binds them to the reducer.
/// </summary>
internal sealed class SettingsControls
{
    private const int LeftColumn = 1;
    private const int RightColumn = 30;

    public SettingsControls(SettingsState state)
    {
        SettingsText text = state.Text;
        Roots.Text = text.Roots;
        WorktreeRoot.Text = text.WorktreeRoot;
        Editor.Value = (int)text.Preset;
        Executable.Text = text.Executable;
        Arguments.Text = text.Arguments;
        BaseRef.Value = text.Base == WorktreeBase.Head ? 1 : 0;
        Place.Value = (int)text.Place;
        Custom = [Caption(RightColumn, 1, "Executable (full path, not a .cmd or .bat)"), Executable, Caption(RightColumn, 4, $"Arguments ({EditorChoice.FolderPlaceholder} is the folder)"), Arguments];
        (IReadOnlyDictionary<SettingsField, View> Fields, View[] All, int Height) layout = state.Scope switch
        {
            SettingsScope.Roots => (
                new Dictionary<SettingsField, View> { [SettingsField.Roots] = Roots },
                [Caption(LeftColumn, 0, "Repository roots (one per line; repositories are found directly under each)"), Roots, Placed(Problem, 7)],
                14),
            SettingsScope.WorktreeRoot => (
                new Dictionary<SettingsField, View> { [SettingsField.WorktreeRoot] = WorktreeRoot },
                [Caption(LeftColumn, 0, "Worktree root (new worktrees go here; the worktrees view lists every worktree wherever it is)"), WorktreeRoot, Placed(Problem, 3)],
                10),
            SettingsScope.Editor => (
                new Dictionary<SettingsField, View> { [SettingsField.Editor] = Editor, [SettingsField.Executable] = Executable, [SettingsField.Arguments] = Arguments },
                [Caption(LeftColumn, 0, "Open folder with (↑↓ then Space to choose)"), Editor, .. Custom, Placed(Problem, 7)],
                14),
            SettingsScope.Base => (
                new Dictionary<SettingsField, View> { [SettingsField.Base] = BaseRef },
                [Caption(LeftColumn, 0, "Worktree base (↑↓ then Space)"), BaseRef, Placed(Problem, 4)],
                10),
            SettingsScope.Place => (
                new Dictionary<SettingsField, View> { [SettingsField.Place] = Place },
                [Caption(LeftColumn, 0, Wording.PlaceCaption), Place, Placed(Problem, 4)],
                10),
            _ => throw new UnreachableException($"{state.Scope.GetType().Name} has no controls."),
        };
        (Fields, All, Height) = layout;
    }

    // TextView is marked obsolete in favour of the separate tui-cs Editor package; a multi-line box does not justify another dependency.
#pragma warning disable CS0618
    public TextView Roots { get; } = new() { X = LeftColumn, Y = 1, Width = Dim.Fill(2), Height = 5, TabKeyAddsTab = false };
#pragma warning restore CS0618

    public TextField WorktreeRoot { get; } = new() { X = LeftColumn, Y = 1, Width = Dim.Fill(2) };

    public OptionSelector Editor { get; } = new() { X = LeftColumn, Y = 1, Labels = [.. Enum.GetValues<EditorPreset>().Select(Wording.Describe)], Value = 0 };

    public TextField Executable { get; } = new() { X = RightColumn, Y = 2, Width = 40 };

    public TextField Arguments { get; } = new() { X = RightColumn, Y = 5, Width = 40 };

    public OptionSelector BaseRef { get; } = new() { X = LeftColumn, Y = 1, Labels = [.. Enum.GetValues<WorktreeBase>().Select(Wording.Describe)], Value = 0 };

    public OptionSelector Place { get; } = new() { X = LeftColumn, Y = 1, Labels = [.. Enum.GetValues<LaunchMode>().Select(Wording.Describe)], Value = 0 };

    public Label Problem { get; } = Text(LeftColumn, Palette.Red, Dim.Fill(1));

    /// <summary>The control that shows each field this dialog edits, so it can follow focus both ways.</summary>
    public IReadOnlyDictionary<SettingsField, View> Fields { get; }

    /// <summary>Every control this dialog shows, in the order Tab visits them.</summary>
    public View[] All { get; }

    /// <summary>The dialog's height for these controls: a Dialog loses four rows to its frame and button row.</summary>
    public int Height { get; }

    /// <summary>The custom editor's captions and fields, shown only while Custom is chosen.</summary>
    private View[] Custom { get; }

    /// <summary>Pushes what the state decides back to the controls: the custom fields, the problem and the focus.</summary>
    public void Show(SettingsState state)
    {
        Array.ForEach(Custom, view => view.Visible = state.ShowsCustomEditor);
        Problem.Text = state.Problem is SettingsProblemLine.Shows shows ? Wording.Describe(shows.Problem) : string.Empty;
        Fields[state.Focus].SetFocus();
    }

    /// <summary>Puts the folders the state holds into their boxes, after the folder picker has added to or replaced them.</summary>
    public void ShowFolders(SettingsState state)
    {
        Roots.Text = state.Text.Roots;
        WorktreeRoot.Text = state.Text.WorktreeRoot;
    }

    /// <summary>Tells the dialog which field took focus, by Tab, a click or the dialog itself.</summary>
    public void WhenFocused(Action<SettingsField> focused)
    {
        foreach ((SettingsField field, View view) in Fields)
        {
            view.HasFocusChanged += (_, _) =>
            {
                if (view.HasFocus)
                {
                    focused(field);
                }
            };
        }
    }

    private static Label Placed(Label label, int y)
    {
        label.Y = y;

        return label;
    }

    private static Label Text(int x, Color colour, Dim width)
    {
        Label label = new() { X = x, Width = width, Height = 2, Text = " " };
        label.SetScheme(Palette.Plain(colour));

        return label;
    }

    private static Label Caption(int x, int y, string text)
    {
        Label label = new() { X = x, Y = y, Text = text };
        label.SetScheme(Palette.Plain(Palette.Orange));

        return label;
    }
}