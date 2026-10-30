using Source191.Vantage.Application;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// The new-session wizard's controls and where they sit, in one column read top to bottom, which Tab visits in order:
/// the repository filter with the list under it, taking the rows the form leaves, then the form anchored to the bottom
/// (the name, Where, the base, the preview, the prompt, the problem and the hint). A fixed folder has no chooser, so its
/// form (the directory, the name, the preview and the prompt) starts at the top and closes up. <see cref="LaunchWizardDialog"/>
/// binds them to the reducer.
/// </summary>
internal sealed class WizardControls
{
    private const int LeftColumn = 1;
    private const int ColumnWidth = 76;

    /// <summary>The rows the one-column form takes under the repository list, counted up from the dialog's bottom.</summary>
    private const int FormRows = 17;

    public WizardControls(WizardState state)
    {
        Mode.Value = state.Mode switch
        {
            LaunchMode.CurrentBranch => 0,
            LaunchMode.NewWorktree => 1,
        };
        BaseRef.Value = state.Base == WorktreeBase.Head ? 1 : 0;
        Hint.Text = Wording.WizardHint(state);
        Fields = new Dictionary<WizardField, View>
        {
            [WizardField.RepositoryFilter] = Filter,
            [WizardField.RepositoryList] = Repositories,
            [WizardField.Name] = Name,
            [WizardField.Mode] = Mode,
            [WizardField.Base] = BaseRef,
            [WizardField.Prompt] = Prompt,
        };
        All = state.ShowsChooser ? OneColumn() : FixedFolder(state);
    }

    public TextField Filter { get; } = new() { X = LeftColumn, Y = 1, Width = ColumnWidth };

    /// <summary>The repositories, taking every row between the filter and the form, never fewer than four.</summary>
    public ListView Repositories { get; } = new() { X = LeftColumn, Y = 2, Width = ColumnWidth, Height = Dim.Fill(FormRows, 4) };

    public Label Chosen { get; } = Text(LeftColumn, 1, Palette.Cyan, ColumnWidth);

    public TextField Name { get; } = new() { X = LeftColumn, Y = 4, Width = ColumnWidth };

    public OptionSelector Mode { get; } = new() { X = LeftColumn, Labels = ["Current branch", "New worktree"] };

    public OptionSelector BaseRef { get; } = new() { X = LeftColumn, Labels = ["origin default branch", "current HEAD"], Value = 0 };

    public Label Preview { get; } = Text(LeftColumn, 6, Palette.Cyan, Dim.Fill(1));

    // TextView is marked obsolete in favour of the separate tui-cs Editor package; a wrapping prompt box does not justify another dependency.
#pragma warning disable CS0618
    public TextView Prompt { get; } = new() { X = LeftColumn, Y = 9, Width = Dim.Fill(2), Height = 5, WordWrap = true, TabKeyAddsTab = false };
#pragma warning restore CS0618

    public Label Problem { get; } = Text(LeftColumn, Pos.AnchorEnd(3), Palette.Red, Dim.Fill(1));

    public Label Hint { get; } = Text(LeftColumn, Pos.AnchorEnd(2), Palette.Blue, Dim.Fill(1));

    /// <summary>The control that shows each field, so the dialog can follow focus both ways.</summary>
    public IReadOnlyDictionary<WizardField, View> Fields { get; }

    /// <summary>Every control in the order Tab visits them, the chooser hidden for a fixed directory.</summary>
    public View[] All { get; }

    private const string PromptCaption = "Opening prompt (wraps; blank leaves the session waiting at its prompt)";

    /// <summary>
    /// The full wizard, top to bottom: the repository caption, the filter and the list, then the form anchored to the
    /// bottom so the list takes what is left. The chosen repository is the list's selection, so it has no label of its own.
    /// </summary>
    private View[] OneColumn()
    {
        Name.Y = Pos.AnchorEnd(16);
        Mode.Y = Pos.AnchorEnd(14);
        BaseRef.Y = Pos.AnchorEnd(11);
        Preview.Y = Pos.AnchorEnd(9);
        Preview.Height = 2;
        Prompt.Y = Pos.AnchorEnd(6);
        Prompt.Height = 3;

        return
        [
            Caption(LeftColumn, 0, "Repository (type to filter, ↑↓ to choose)"),
            Filter,
            Repositories,
            Caption(LeftColumn, Pos.AnchorEnd(FormRows), "Session name (required)"),
            Name,
            Caption(LeftColumn, Pos.AnchorEnd(15), "Where (↑↓ to change)"),
            Mode,
            Caption(LeftColumn, Pos.AnchorEnd(12), "Worktree base (↑↓ to change)"),
            BaseRef,
            Preview,
            Caption(LeftColumn, Pos.AnchorEnd(7), PromptCaption),
            Prompt,
            Problem,
            Hint,
        ];
    }

    /// <summary>A fixed folder: the directory, the name, a two-line preview and the prompt from the top, with no chooser or Where.</summary>
    private View[] FixedFolder(WizardState state) =>
    [
        Caption(LeftColumn, 0, Wording.WizardChosenCaption(state)),
        Chosen,
        Caption(LeftColumn, 3, "Session name (required)"),
        Name,
        Preview,
        Caption(LeftColumn, 8, PromptCaption),
        Prompt,
        Problem,
        Hint,
    ];

    /// <summary>How far an arrow or page key typed in the filter moves the list; null for any other key.</summary>
    public int? ListDelta(GuiKey key)
    {
        int page = Repositories.Viewport.Height;
        (GuiKey Key, int Delta)[] moves = [(GuiKey.CursorDown, 1), (GuiKey.CursorUp, -1), (GuiKey.PageDown, page), (GuiKey.PageUp, -page)];

        return moves.Where(move => move.Key == key).Select(move => (int?)move.Delta).FirstOrDefault();
    }

    /// <summary>Tells the dialog which field took focus, by Tab, a click or the dialog itself.</summary>
    public void WhenFocused(Action<WizardField> focused)
    {
        foreach ((WizardField field, View view) in Fields)
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

    /// <summary>Runs the action for Enter on any option of the Where and base selectors, instead of the option taking it.</summary>
    public void WhenEnterInSelectors(Action next)
    {
        foreach (View option in Mode.SubViews.Concat(BaseRef.SubViews).Append(Mode).Append(BaseRef))
        {
            option.KeyDown += (_, key) => key.Handled = key == GuiKey.Enter && Handled(next);
        }
    }

    /// <summary>Runs a key's or button's action and marks the event handled.</summary>
    public static bool Handled(Action action)
    {
        action();

        return true;
    }

    public static Button Button(string text, Action pressed)
    {
        Button button = new() { Text = text };
        button.Accepting += (_, accepting) => accepting.Handled = Handled(pressed);

        return button;
    }

    private static Label Text(int x, Pos y, Color colour, Dim width)
    {
        Label label = new() { X = x, Y = y, Width = width, Text = string.Empty };
        label.SetScheme(Palette.Plain(colour));

        return label;
    }

    private static Label Caption(int x, Pos y, string text)
    {
        Label label = new() { X = x, Y = y, Text = text };
        label.SetScheme(Palette.Plain(Palette.Orange));

        return label;
    }
}