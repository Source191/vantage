using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The settings dialog's controls and where they sit: the roots and the worktree root across the top, the editor on
/// the left with a custom editor's command beside it, and the worktree base and the update check on the right. <see cref="SettingsDialog"/>
/// binds them to the reducer.
/// </summary>
internal sealed class SettingsControls
{
    private const int LeftColumn = 1;
    private const int RightColumn = 30;
    private const int BaseColumn = 72;

    public SettingsControls(SettingsState state)
    {
        SettingsText text = state.Text;
        Roots.Text = text.Roots;
        WorktreeRoot.Text = text.WorktreeRoot;
        Editor.Value = (int)text.Preset;
        Executable.Text = text.Executable;
        Arguments.Text = text.Arguments;
        BaseRef.Value = text.Base == WorktreeBase.Head ? 1 : 0;
        CheckForUpdates.Value = text.CheckForUpdates ? CheckState.Checked : CheckState.UnChecked;
        Fields = new Dictionary<SettingsField, View>
        {
            [SettingsField.Roots] = Roots,
            [SettingsField.WorktreeRoot] = WorktreeRoot,
            [SettingsField.Editor] = Editor,
            [SettingsField.Executable] = Executable,
            [SettingsField.Arguments] = Arguments,
            [SettingsField.Base] = BaseRef,
            [SettingsField.CheckForUpdates] = CheckForUpdates,
        };
        Custom = [Caption(RightColumn, 10, "Executable (full path, not a .cmd or .bat)"), Executable, Caption(RightColumn, 13, $"Arguments ({EditorChoice.FolderPlaceholder} is the folder)"), Arguments];
        All =
        [
            Caption(LeftColumn, 0, "Repository roots (one per line; repositories are found directly under each)"),
            Roots,
            Caption(LeftColumn, 6, "Worktree root (new worktrees go here; the worktrees view lists every worktree wherever it is)"),
            WorktreeRoot,
            Caption(LeftColumn, 9, "Open folder with (↑↓ then Space to choose)"),
            Editor,
            .. Custom,
            Caption(BaseColumn, 9, "Worktree base (↑↓ then Space)"),
            BaseRef,
            CheckForUpdates,
            Problem,
        ];
    }

    // TextView is marked obsolete in favour of the separate tui-cs Editor package; a multi-line box does not justify another dependency.
#pragma warning disable CS0618
    public TextView Roots { get; } = new() { X = LeftColumn, Y = 1, Width = Dim.Fill(2), Height = 4, TabKeyAddsTab = false };
#pragma warning restore CS0618

    public TextField WorktreeRoot { get; } = new() { X = LeftColumn, Y = 7, Width = Dim.Fill(2) };

    public OptionSelector Editor { get; } = new() { X = LeftColumn, Y = 10, Labels = [.. Enum.GetValues<EditorPreset>().Select(Wording.Describe)], Value = 0 };

    public TextField Executable { get; } = new() { X = RightColumn, Y = 11, Width = 40 };

    public TextField Arguments { get; } = new() { X = RightColumn, Y = 14, Width = 40 };

    public OptionSelector BaseRef { get; } = new() { X = BaseColumn, Y = 10, Labels = ["origin default branch", "current HEAD"], Value = 0 };

    /// <summary>Space toggles it; nuget.org is asked at most once a day while it is checked.</summary>
    public CheckBox CheckForUpdates { get; } = new() { X = BaseColumn, Y = 13, Text = "Check for updates daily" };

    public Label Problem { get; } = Text(LeftColumn, 17, Palette.Red, Dim.Fill(1));

    /// <summary>The control that shows each field, so the dialog can follow focus both ways.</summary>
    public IReadOnlyDictionary<SettingsField, View> Fields { get; }

    /// <summary>Every control in the order Tab visits them.</summary>
    public View[] All { get; }

    /// <summary>The custom editor's captions and fields, shown only while Custom is chosen.</summary>
    private View[] Custom { get; }

    /// <summary>Pushes what the state decides back to the controls: the custom fields, the problem and the focus.</summary>
    public void Show(SettingsState state)
    {
        Array.ForEach(Custom, view => view.Visible = state.ShowsCustomEditor);
        Problem.Text = state.Problem is null ? string.Empty : Wording.Describe(state.Problem);
        Fields[state.Focus].SetFocus();
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

    private static Label Text(int x, Pos y, Color colour, Dim width)
    {
        Label label = new() { X = x, Y = y, Width = width, Text = string.Empty };
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