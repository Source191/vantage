using Source191.Vantage.Actions;
using Source191.Vantage.Domain;
using Source191.Vantage.Launch;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Screens.Tui;

/// <summary>What the worktrees screen hands back when it stops: the state to resume with and the effect for the caller.</summary>
public sealed record WorktreesScreenExit(WorktreesViewState State, ViewEffect? Effect);

/// <summary>
/// Renders the worktrees view with Terminal.Gui, the same way <see cref="SessionsScreen"/> renders sessions: keys go
/// to the reducer, state is redrawn, and effects that need another screen or the terminal stop the application.
/// </summary>
public sealed class WorktreesScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    WorktreeActions actions,
    LaunchWorkflow launches,
    string defaultBase,
    TimeProvider clock,
    string context)
{
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromSeconds(15);

    private const string HelpText = """
        d      remove (refused when work would be lost)   D  force remove after typing the branch or folder name
        p      prune stale registrations in its repository
        s      start a session in this worktree            o  open a PowerShell tab there
        Enter  go to the session running in it             w or Esc  back to sessions
        /      filter    ?  help    q  quit
        Health: ok, locked (by another tool), orphan (folder git forgot), broken (registration or owner gone), empty (leftover folder)
        """;

    public WorktreesScreenExit Run(WorktreesViewState initial)
    {
        using IApplication app = Application.Create().Init();
        if (app.Driver is not null)
        {
            app.Driver.Force16Colors = false;
        }

        using Live live = Live.Build(initial, context);
        live.Window.KeyDown += (_, key) => OnKey(app, live, key);
        live.Redraw(clock.GetUtcNow());

        object? timer = app.AddTimeout(RefreshEvery, () =>
        {
            TriggerLoad(app, live);
            return true;
        });
        TriggerLoad(app, live);

        app.Run(live.Window);
        if (timer is not null)
        {
            app.RemoveTimeout(timer);
        }

        return new WorktreesScreenExit(live.State, live.Exit);
    }

    private void TriggerLoad(IApplication app, Live live)
    {
        Task.Run(async () =>
        {
            ViewInput input;
            try
            {
                input = await load(CancellationToken.None);
            }
            catch (Exception exception)
            {
                input = new LoadFailed(exception.Message);
            }

            app.Invoke(() => Apply(app, live, input));
        });
    }

    private void OnKey(IApplication app, Live live, GuiKey key)
    {
        Key? mapped = KeyMapping.From(key);
        if (mapped is null)
        {
            return;
        }

        key.Handled = true;
        Apply(app, live, new KeyPressed(mapped));
    }

    private void Apply(IApplication app, Live live, ViewInput input)
    {
        WorktreesViewUpdate update = WorktreesView.Reduce(live.State, input);
        live.State = update.State;
        foreach (ViewEffect effect in update.Effects)
        {
            if (effect is SwitchToSessions or JumpToSession or Quit or AttachToSession)
            {
                live.Exit = effect;
                app.RequestStop();
                return;
            }

            switch (effect)
            {
                case RemoveWorktree remove:
                    Remove(app, live, remove.Row, remove.Forced);
                    break;
                case ConfirmForceRemove confirm:
                    ConfirmThenForce(app, live, confirm);
                    break;
                case PruneRepository prune:
                    RunInBackground(app, live, "Pruning...", token => actions.PruneAsync(prune.Repository, token));
                    break;
                case OpenShellTab shell:
                    RunInBackground(app, live, $"Opening a shell in {shell.Row.FolderName}...", token => actions.OpenShellAsync(shell.Row, token));
                    break;
                case StartSessionIn start:
                    RunWizard(app, live, start.Row);
                    break;
                case ShowHelp:
                    Confirm.Query(app, "Keys", HelpText, "Close");
                    break;
                default:
                    break;
            }
        }

        live.Redraw(clock.GetUtcNow());
    }

    private void ConfirmThenForce(IApplication app, Live live, ConfirmForceRemove confirm)
    {
        string? typed = TypedConfirmationDialog.Show(
            app,
            "Force remove",
            "This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:",
            confirm.ConfirmationText,
            "folder name");
        Apply(app, live, new ForceConfirmed(confirm.Row, confirm.ConfirmationText, typed));
    }

    private void Remove(IApplication app, Live live, WorktreeRow row, bool forced)
    {
        RunInBackground(
            app,
            live,
            $"Removing {row.FolderName}...",
            token => actions.RemoveAsync(row, forced, token),
            outcome =>
            {
                if (outcome.BranchToConfirm is null)
                {
                    return outcome.Message;
                }

                int? answer = Confirm.Query(app, "Delete branch", $"Branch {outcome.BranchToConfirm.Branch} was not created by Vantage. Delete it anyway?", "Delete branch", "Keep");
                if (answer != 0)
                {
                    return $"{outcome.Message} Branch kept.";
                }

                RunInBackground(app, live, $"Deleting branch {outcome.BranchToConfirm.Branch}...", async token =>
                {
                    await actions.DeleteBranchAsync(outcome.BranchToConfirm, token);
                    return $"{outcome.Message} Branch deleted.";
                });

                return $"Deleting branch {outcome.BranchToConfirm.Branch}...";
            });
    }

    private void RunWizard(IApplication app, Live live, WorktreeRow row)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(app, launches, defaultBase, fixedDirectory: row.Path);
        if (wizard is null)
        {
            return;
        }

        RunInBackground(
            app,
            live,
            $"Starting {wizard.Plan.Name.Text}...",
            token => launches.LaunchAsync(wizard.Plan, token),
            outcome =>
            {
                if (outcome.Kind == LaunchOutcomeKind.Started && wizard.AttachAfter && outcome.JobId is not null)
                {
                    live.Exit = new AttachToSession(outcome.JobId);
                    app.RequestStop();
                }

                return outcome.Kind switch
                {
                    LaunchOutcomeKind.Started => $"Started {wizard.Plan.Name.Text} ({outcome.JobId}).",
                    LaunchOutcomeKind.WorkspaceNotTrusted => $"Could not start: {wizard.Plan.Directory} is not trusted yet. Run claude there once and accept the prompt.",
                    _ => $"Could not start: {outcome.Message}",
                };
            });
    }

    private void RunInBackground(IApplication app, Live live, string working, Func<CancellationToken, Task<string>> action)
    {
        RunInBackground(app, live, working, action, footer => footer);
    }

    /// <summary>Runs the action off the UI thread, then the continuation on it; the continuation's text becomes the footer.</summary>
    private void RunInBackground<T>(IApplication app, Live live, string working, Func<CancellationToken, Task<T>> action, Func<T, string> onUiThread)
    {
        live.State = live.State with { Footer = working };
        live.Redraw(clock.GetUtcNow());
        Task.Run(async () =>
        {
            T? result = default;
            string? failure = null;
            try
            {
                result = await action(CancellationToken.None);
            }
            catch (Exception exception)
            {
                failure = $"Failed: {exception.Message}";
            }

            app.Invoke(() =>
            {
                string footer = failure ?? onUiThread(result!);
                live.State = live.State with { Footer = footer };
                live.Redraw(clock.GetUtcNow());
                TriggerLoad(app, live);
            });
        });
    }

    /// <summary>The views alive during one Run, plus the state they show.</summary>
    private sealed class Live : IDisposable
    {
        private readonly TableView table;
        private readonly Label contextValues;
        private readonly string context;
        private readonly Label title;
        private readonly Label banner;
        private readonly Label footer;

        private Live(Window window, TableView table, Label contextValues, string context, Label title, Label banner, Label footer, WorktreesViewState state)
        {
            Window = window;
            this.table = table;
            this.contextValues = contextValues;
            this.context = context;
            this.title = title;
            this.banner = banner;
            this.footer = footer;
            State = state;
        }

        public Window Window { get; }

        public WorktreesViewState State { get; set; }

        public ViewEffect? Exit { get; set; }

        public static Live Build(WorktreesViewState state, string context)
        {
            Window window = new() { BorderStyle = LineStyle.None };
            window.SetScheme(Palette.Plain(Palette.White));

            Label contextNames = Text(1, 0, 11, "Machine:\nClaude:\nRoots:\nWorktrees:\nAt risk:", Palette.Orange);
            Label contextValues = Text(13, 0, 48, context, Palette.White);
            Label keys = Text(62, 0, 10, "<d>\n<D>\n<p>\n<s>\n<o>\n<enter>", Palette.Blue);
            Label keyNames = Text(72, 0, 16, "Remove\nForce remove\nPrune\nStart session\nOpen shell\nGo to session", Palette.White);
            Label keys2 = Text(90, 0, 8, "<w>\n<esc>\n</>\n<?>\n<q>", Palette.Blue);
            Label keyNames2 = Text(98, 0, 20, "Sessions\nSessions\nFilter\nHelp\nQuit", Palette.White);
            Label logo = Text(Pos.AnchorEnd(12), 0, 11, " VANTAGE\n ◢◤◢◤◢◤\n", Palette.Orange);

            Label banner = Text(1, 6, Dim.Fill(), string.Empty, Palette.Red);
            Label title = Text(2, 7, Dim.Fill(), string.Empty, Palette.Cyan);

            TableView table = new()
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill(),
                Height = Dim.Fill(3),
                CanFocus = false,
                FullRowSelect = true,
                MultiSelect = false,
                BorderStyle = LineStyle.Rounded,
            };
            table.Style.ShowVerticalCellLines = false;
            table.Style.ShowHorizontalHeaderOverline = false;
            table.Style.ExpandLastColumn = true;
            table.SetScheme(Palette.Plain(Palette.White));

            Label crumb = Text(1, Pos.AnchorEnd(2), 13, " <worktrees> ", Palette.Background);
            crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
            Label footer = Text(15, Pos.AnchorEnd(2), Dim.Fill(), string.Empty, Palette.Cyan);

            window.Add(contextNames, contextValues, keys, keyNames, keys2, keyNames2, logo, banner, title, table, crumb, footer);

            Live live = new(window, table, contextValues, context, title, banner, footer, state);
            table.Style.RowColorGetter = live.RowColour;

            return live;
        }

        public void Redraw(DateTimeOffset now)
        {
            IReadOnlyList<WorktreeRow> rows = State.VisibleRows;
            table.Table = new WorktreeTableSource(rows, now);
            contextValues.Text = $"{context}\n{Summary()}\n{AtRisk()}";
            title.Text = State.FilterEditing ? $"{State.Title}  filter: {State.Filter.Text}_" : State.Title;
            banner.Text = State.Banner is null ? string.Empty : $"  scan failed: {State.Banner}";
            footer.Text = State.FooterText;
            int selected = Math.Max(rows.ToList().FindIndex(row => string.Equals(row.Path, State.SelectedPath, StringComparison.OrdinalIgnoreCase)), 0);
            if (rows.Count > 0)
            {
                table.SetSelection(0, selected, false, null);
            }

            Window.SetNeedsDraw();
        }

        private string Summary()
        {
            IEnumerable<string> byHealth = State.Rows
                .GroupBy(row => row.Health.Kind)
                .OrderByDescending(group => group.Count())
                .Select(group => $"{group.Count()} {group.Key.ToString().ToLowerInvariant()}");

            return State.Rows.Count == 0 ? "none" : $"{State.Rows.Count} ({string.Join(", ", byHealth)})";
        }

        private string AtRisk()
        {
            int dirty = State.Rows.Count(row => row.DirtyFiles > 0);
            int unpushed = State.Rows.Count(row => row.UnpushedCommits > 0);

            return $"{dirty} with changes, {unpushed} with unpushed commits";
        }

        public void Dispose()
        {
            Window.Dispose();
        }

        private Scheme? RowColour(RowColorGetterArgs args)
        {
            if (args.Table is not WorktreeTableSource source)
            {
                return null;
            }

            WorktreeRow row = source.RowAt(args.RowIndex);

            return string.Equals(row.Path, State.SelectedPath, StringComparison.OrdinalIgnoreCase)
                ? Palette.Highlight()
                : Palette.Plain(ColourFor(row.Health.Kind));
        }

        private static Color ColourFor(WorktreeHealthKind kind)
        {
            return kind switch
            {
                WorktreeHealthKind.Ok => Palette.White,
                WorktreeHealthKind.Locked => Palette.Amber,
                WorktreeHealthKind.Empty => Palette.Grey,
                _ => Palette.Red,
            };
        }

        private static Label Text(Pos x, Pos y, Dim width, string text, Color colour)
        {
            Label label = new() { X = x, Y = y, Width = width, Height = Dim.Auto(), Text = text };
            label.SetScheme(Palette.Plain(colour));

            return label;
        }
    }
}