using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// The new-session wizard's controls and where they sit. The left column chooses the repository; the right-hand
/// column reads top to bottom like a form. <see cref="LaunchWizardDialog"/> binds them to the reducer.
/// </summary>
internal sealed class WizardControls
{
    private const int LeftColumn = 1;
    private const int RightColumn = 48;
    private const int ColumnWidth = 44;

    public WizardControls(WizardState state)
    {
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
        View[] chooser = [Caption(LeftColumn, 0, "Repository (type to filter, ↑↓ to choose)"), Filter, Repositories, Caption(RightColumn, 6, "Where (↑↓ to change)"), Mode, Caption(RightColumn, 9, "Worktree base (↑↓ to change)"), BaseRef];
        Array.ForEach(chooser, view => view.Visible = state.ShowsChooser);
        All =
        [
            .. chooser,
            Caption(RightColumn, 0, Wording.WizardChosenCaption(state)),
            Chosen,
            Caption(RightColumn, 3, "Session name (required)"),
            Name,
            Preview,
            Caption(RightColumn, 14, "Opening prompt (wraps; blank opens on the agent list until you send one)"),
            Prompt,
            Problem,
            Hint,
        ];
    }

    public TextField Filter { get; } = new() { X = LeftColumn, Y = 1, Width = ColumnWidth };

    public ListView Repositories { get; } = new() { X = LeftColumn, Y = 2, Width = ColumnWidth, Height = Dim.Fill(4) };

    public Label Chosen { get; } = Text(RightColumn, 1, Palette.Cyan, ColumnWidth);

    public TextField Name { get; } = new() { X = RightColumn, Y = 4, Width = ColumnWidth };

    public OptionSelector Mode { get; } = new() { X = RightColumn, Y = 7, Labels = ["Current branch", "New sibling worktree"], Value = 0 };

    public OptionSelector BaseRef { get; } = new() { X = RightColumn, Y = 10, Labels = ["origin default branch", "current HEAD"], Value = 0 };

    public Label Preview { get; } = Text(RightColumn, 12, Palette.Cyan, Dim.Fill(1));

    // TextView is marked obsolete in favour of the separate tui-cs Editor package; a wrapping prompt box does not justify another dependency.
#pragma warning disable CS0618
    public TextView Prompt { get; } = new() { X = RightColumn, Y = 15, Width = Dim.Fill(2), Height = 5, WordWrap = true, TabKeyAddsTab = false };
#pragma warning restore CS0618

    public Label Problem { get; } = Text(LeftColumn, Pos.AnchorEnd(3), Palette.Red, Dim.Fill(1));

    public Label Hint { get; } = Text(LeftColumn, Pos.AnchorEnd(2), Palette.Blue, Dim.Fill(1));

    /// <summary>The control that shows each field, so the dialog can follow focus both ways.</summary>
    public IReadOnlyDictionary<WizardField, View> Fields { get; }

    /// <summary>Every control in the order Tab visits them, the chooser hidden for a fixed directory.</summary>
    public View[] All { get; }

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

    private static Label Caption(int x, int y, string text)
    {
        Label label = new() { X = x, Y = y, Text = text };
        label.SetScheme(Palette.Plain(Palette.Orange));

        return label;
    }
}