using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>What the wizard produces: a validated plan and whether to open the session in its own tab as soon as it starts.</summary>
public sealed record WizardResult(LaunchPlan Plan, bool OpenInTab);

/// <summary>
/// The new-session dialog. Collects the request, validates it through the plan builder, and hands the plan back.
/// Nothing is started here; the screen runs the launch so the dialog can close at once.
/// Keyboard model: focus starts in the repository filter; typing narrows the list, the arrow keys move into the
/// list (whose selected row stays visible after focus leaves it), and Enter confirms the repository and moves to
/// the name. Enter in the other fields moves on to the next one; only F5 (start) and F6 (start and open in a tab) launch,
/// so a stray Enter never starts a half-filled session.
/// </summary>
internal static class LaunchWizardDialog
{
    private const int LeftColumn = 1;
    private const int RightColumn = 48;
    private const int ColumnWidth = 44;
    private const int PromptLines = 5;

    /// <summary>Shows the wizard. With a fixed directory (the worktrees view's s key) the session starts there: no repository list, no worktree choice.</summary>
    public static WizardResult? Show(IApplication app, LaunchWorkflow workflow, WorktreeBase defaultBase, string? fixedDirectory = null)
    {
        WizardResult? result = null;
        RepositoryChoice? fixedChoice = fixedDirectory is null ? null : new RepositoryChoice(new FolderPath(fixedDirectory).Name, fixedDirectory, IsRepository: true);
        using Dialog dialog = new() { Title = fixedChoice is null ? "New session" : $"New session in {fixedChoice.Name}", Width = Dim.Percent(90), Height = Dim.Percent(90) };
        dialog.SetScheme(Palette.Focusable(Palette.White));

        // Read once, on a pool thread: Terminal.Gui installs a synchronisation context while it runs, so awaiting
        // on the UI thread and then blocking for the result would deadlock (it did, after every attach-and-return).
        IReadOnlyList<string> recent = Task.Run(workflow.RecentAsync).GetAwaiter().GetResult();
        IReadOnlyList<RepositoryChoice> choices = [];
        TextField repositoryFilter = new() { X = LeftColumn, Y = 1, Width = ColumnWidth };
        ListView repositories = new() { X = LeftColumn, Y = 2, Width = ColumnWidth, Height = Dim.Fill(4) };
        void Refilter(string filter)
        {
            choices = workflow.Choices(recent, filter);
            repositories.SetSource(new ObservableCollection<string>(choices.Select(choice => choice.Name)));
            repositories.SelectedItem = choices.Count > 0 ? 0 : null;
        }

        RepositoryChoice? Chosen()
        {
            int selected = repositories.SelectedItem ?? -1;

            return fixedChoice ?? (selected >= 0 && selected < choices.Count ? choices[selected] : null);
        }

        // The right-hand column reads top to bottom like a form: the repository just chosen, then the name and so on.
        Label chosen = new() { X = RightColumn, Y = 1, Width = ColumnWidth, Text = string.Empty };
        chosen.SetScheme(Palette.Plain(Palette.Cyan));
        TextField name = new() { X = RightColumn, Y = 4, Width = ColumnWidth };
        TextField description = new() { X = RightColumn, Y = 7, Width = ColumnWidth };
        OptionSelector mode = new() { X = RightColumn, Y = 10, Labels = ["Current branch", "New sibling worktree"], Value = 0 };
        OptionSelector baseRef = new() { X = RightColumn, Y = 13, Labels = ["origin default branch", "current HEAD"], Value = defaultBase == WorktreeBase.Head ? 1 : 0 };
        Label preview = new() { X = RightColumn, Y = 15, Width = Dim.Fill(1), Text = string.Empty };
        preview.SetScheme(Palette.Plain(Palette.Cyan));
        // TextView is marked obsolete in favour of the separate tui-cs Editor package; a wrapping prompt box does not justify another dependency.
#pragma warning disable CS0618
        TextView prompt = new() { X = RightColumn, Y = 18, Width = Dim.Fill(2), Height = PromptLines, WordWrap = true, TabKeyAddsTab = false };
#pragma warning restore CS0618
        Label problem = new() { X = LeftColumn, Y = Pos.AnchorEnd(3), Width = Dim.Fill(1), Text = string.Empty };
        problem.SetScheme(Palette.Plain(Palette.Red));
        Label hint = new() { X = LeftColumn, Y = Pos.AnchorEnd(2), Width = Dim.Fill(1), Text = "Type to filter, ↑↓ to choose, Enter confirms the repository   Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel" };
        hint.SetScheme(Palette.Plain(Palette.Blue));
        Label repositoryCaption = Caption(LeftColumn, 0, "Repository (type to filter, ↑↓ to choose)");
        Label whereCaption = Caption(RightColumn, 9, "Where (↑↓ to change)");
        Label baseCaption = Caption(RightColumn, 12, "Worktree base (↑↓ to change)");
        if (fixedChoice is not null)
        {
            // The directory is settled, so the chooser and the worktree options have nothing to offer.
            foreach (View hidden in new View[] { repositoryCaption, repositoryFilter, repositories, whereCaption, mode, baseCaption, baseRef })
            {
                hidden.Visible = false;
            }

            hint.Text = "Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel";
        }

        void ShowPreview(string? nameText = null)
        {
            RepositoryChoice? repository = Chosen();
            LaunchMode launchMode = mode.Value == 1 ? LaunchMode.NewWorktree : LaunchMode.CurrentBranch;
            preview.Text = repository is null ? string.Empty : LaunchPreview.Describe(repository, nameText ?? name.Text, launchMode);
        }

        void ShowChosen()
        {
            chosen.Text = Chosen()?.Name ?? "(none)";
            ShowPreview();
        }

        Refilter(string.Empty);
        ShowChosen();
        repositoryFilter.TextChanging += (_, changing) => Refilter(changing.Result ?? string.Empty);
        repositories.ValueChanged += (_, _) => ShowChosen();
        name.TextChanging += (_, changing) => ShowPreview(changing.Result);
        mode.ValueChanged += (_, _) => ShowPreview();

        // One list, two ways in: arrows typed in the filter move focus to the list (so the highlight is visible),
        // and letters typed in the list go back to the filter. Enter in either confirms the repository and moves to Name.
        repositoryFilter.KeyDown += (_, key) =>
        {
            if (IsListNavigation(key))
            {
                repositories.SetFocus();
                key.Handled = MoveSelection(repositories, key);
            }
        };
        repositories.KeyDown += (_, key) =>
        {
            if (key == GuiKey.Backspace || (!key.IsAlt && !key.IsCtrl && key.AsRune.Value >= ' '))
            {
                repositoryFilter.SetFocus();
                key.Handled = repositoryFilter.NewKeyDownEvent(key);
            }
        };

        dialog.Add(
            repositoryCaption,
            repositoryFilter,
            repositories,
            Caption(RightColumn, 0, fixedChoice is null ? "Repository" : "Directory"),
            chosen,
            Caption(RightColumn, 3, "Session name (required)"),
            name,
            Caption(RightColumn, 6, "Description (optional)"),
            description,
            whereCaption,
            mode,
            baseCaption,
            baseRef,
            preview,
            Caption(RightColumn, 17, "Opening prompt (wraps; blank opens on the agent list until you send one)"),
            prompt,
            problem,
            hint);

        // No default button: Enter never starts anything by itself. The function keys and the buttons do.
        Button start = new() { Text = "_Start (F5)" };
        Button startInTab = new() { Text = "Start and open in _tab (F6)" };
        Button cancel = new() { Text = "_Cancel (Esc)" };
        start.Accepting += (_, accepting) => accepting.Handled = TryFinish(openInTab: false);
        startInTab.Accepting += (_, accepting) => accepting.Handled = TryFinish(openInTab: true);
        cancel.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.KeyDown += (_, key) =>
        {
            if (key == GuiKey.F5)
            {
                key.Handled = TryFinish(openInTab: false);
            }
            else if (key == GuiKey.F6)
            {
                key.Handled = TryFinish(openInTab: true);
            }
        };
        foreach (View chooser in new View[] { repositoryFilter, repositories })
        {
            chooser.Accepting += (_, accepting) => accepting.Handled = ConfirmRepository();
        }

        // Enter in a one-line field moves on, like Tab, instead of accepting the dialog.
        name.Accepting += (_, accepting) => accepting.Handled = Focus(description);
        description.Accepting += (_, accepting) => accepting.Handled = Focus(mode);

        dialog.AddButton(start);
        dialog.AddButton(startInTab);
        dialog.AddButton(cancel);

        app.Run(dialog);

        return result;

        bool ConfirmRepository()
        {
            if (Chosen() is null)
            {
                return Fail("Choose a repository: type part of its name, then ↑↓.", repositoryFilter);
            }

            problem.Text = string.Empty;
            ShowChosen();
            name.SetFocus();
            return true;
        }

        bool TryFinish(bool openInTab)
        {
            RepositoryChoice? repository = Chosen();
            if (repository is null)
            {
                return Fail("Choose a repository: type part of its name, then ↑↓.", repositoryFilter);
            }

            if (string.IsNullOrWhiteSpace(name.Text))
            {
                return Fail("Session name is required: a short, human-readable label for the session.", name);
            }

            LaunchRequest request = new(
                repository,
                name.Text,
                description.Text,
                mode.Value == 1 ? LaunchMode.NewWorktree : LaunchMode.CurrentBranch,
                baseRef.Value == 1 ? WorktreeBase.Head : WorktreeBase.RemoteDefault,
                prompt.Text);
            LaunchPlanResult planned = workflow.Plan(request);
            if (planned.Plan is null)
            {
                return Fail(planned.Problem ?? "That cannot be launched.", name);
            }

            result = new WizardResult(planned.Plan, openInTab);
            app.RequestStop(dialog);
            return true;
        }

        bool Fail(string message, View field)
        {
            problem.Text = message;
            field.SetFocus();
            return true;
        }
    }

    private static bool Focus(View field)
    {
        field.SetFocus();

        return true;
    }

    private static bool IsListNavigation(GuiKey key)
    {
        return key == GuiKey.CursorDown || key == GuiKey.CursorUp || key == GuiKey.PageDown || key == GuiKey.PageUp;
    }

    /// <summary>Arrow and page keys typed in the filter drive the list, so choosing never needs a Tab first.</summary>
    private static bool MoveSelection(ListView list, GuiKey key)
    {
        if (key == GuiKey.CursorDown)
        {
            return list.MoveDown();
        }

        if (key == GuiKey.CursorUp)
        {
            return list.MoveUp();
        }

        if (key == GuiKey.PageDown)
        {
            return list.MovePageDown();
        }

        if (key == GuiKey.PageUp)
        {
            return list.MovePageUp();
        }

        return false;
    }

    private static Label Caption(int x, int y, string text)
    {
        Label label = new() { X = x, Y = y, Text = text };
        label.SetScheme(Palette.Plain(Palette.Orange));

        return label;
    }
}