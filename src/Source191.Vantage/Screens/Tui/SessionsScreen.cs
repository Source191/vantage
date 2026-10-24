using Source191.Vantage.Actions;
using Source191.Vantage.Domain;
using Source191.Vantage.Launch;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Screens.Tui;

/// <summary>What the screen hands back when it stops: the state to resume with and the effect that needs the terminal.</summary>
public sealed record ScreenExit(SessionsViewState State, ViewEffect? Effect);

/// <summary>
/// Renders the sessions view with Terminal.Gui. Keys go to the reducer; the reducer's state is redrawn; effects
/// that need the terminal stop the application and are returned to the caller to run.
/// </summary>
public sealed class SessionsScreen(
    Func<CancellationToken, Task<ViewInput>> load,
    StateTransitionNotifier notifier,
    LaunchWorkflow launches,
    SessionActions actions,
    TabOpener tabs,
    string defaultBase,
    TimeProvider clock,
    string context)
{
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromSeconds(5);

    public ScreenExit Run(SessionsViewState initial)
    {
        using IApplication app = Application.Create().Init();
        // Windows Terminal renders 24-bit colour; without this the driver falls back to 16 colours and the palette shifts.
        if (app.Driver is not null)
        {
            app.Driver.Force16Colors = false;
        }
        using Live live = Live.Build(app, initial, context);
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

        return new ScreenExit(live.State, live.Exit);
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
        ViewUpdate update = SessionsView.Reduce(live.State, input);
        if (input is RowsLoaded loaded)
        {
            notifier.Observe(live.State.Rows, loaded.Rows);
        }

        live.State = update.State;
        foreach (ViewEffect effect in update.Effects)
        {
            if (effect is AttachToSession or OpenAgentView or Quit or SwitchToWorktrees)
            {
                live.Exit = effect;
                app.RequestStop();
                return;
            }

            switch (effect)
            {
                case OpenLaunchWizard:
                    RunWizard(app, live);
                    break;
                case OpenTabForSession open:
                    RunInBackground(app, live, $"Opening a tab for {open.Row.Session.Name}...", token => tabs.OpenAsync(open.Row, token));
                    break;
                case StopSession stop:
                    RunInBackground(app, live, "Stopping...", token => actions.StopAsync(live.State.Rows.First(row => row.Session.JobId == stop.JobId), token));
                    break;
                case AdoptSession adopt:
                    RunInBackground(app, live, $"Moving {adopt.Row.Session.Name} under Vantage...", token => actions.AdoptAsync(adopt.Row, token));
                    break;
                case CloseEmptyTerminals close:
                    CloseEmptyTerminals(app, live, close.Rows);
                    break;
                case AskForNewName ask:
                    string? typed = NameDialog.Show(app, "Rename session", $"Rename {ask.Row.Session.Name}. The session restarts under the new name and keeps its conversation.", ask.Row.Session.Name);
                    Apply(app, live, new RenameConfirmed(ask.Row, typed));
                    return;
                case RenameSession rename:
                    RunInBackground(app, live, $"Renaming {rename.Row.Session.Name} to {rename.NewName.Text}...", token => actions.RenameAsync(rename.Row, rename.NewName, token));
                    break;
                case DeleteSession delete:
                    ConfirmAndDelete(app, live, delete.Row);
                    break;
                case ResumeStoppedSessions:
                    ResumeSessions(app, live);
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

    private const string HelpText = """
        Enter  attach (Ctrl+Z/Esc back)    o  open in its own tab
        n      new session                 r  rename (restarts the session under the new name)
        x      stop                        d  delete (and its worktree, safely); closes the tab of a terminal session with no messages
        C      close every tab whose claude has never been sent a message
        b      move a terminal session under Vantage (its tab becomes a shell)
        R      resume after a reboot       a  native agent view
        w      worktrees view              /  filter    ?  help    q  quit
        """;

    private void CloseEmptyTerminals(IApplication app, Live live, IReadOnlyList<SessionRow> rows)
    {
        string names = string.Join(", ", rows.Select(row => row.Session.Name));
        string what = rows.Count == 1 ? $"Close the tab of {names}?" : $"Close {rows.Count} tabs: {names}?";
        int? answer = Confirm.Query(app, "Close empty terminal sessions", $"{what}\nNone of them has been sent a message, so nothing is lost.", "Close", "Keep");
        if (answer != 0)
        {
            live.State = live.State with { Footer = "Kept." };
            return;
        }

        RunInBackground(app, live, "Closing...", token => actions.CloseEmptyTerminalsAsync(rows, token));
    }

    private void ConfirmAndDelete(IApplication app, Live live, SessionRow row)
    {
        int? answer = Confirm.Query(app, "Delete session", $"Delete {row.Session.Name}?\nIts worktree is removed only when nothing in it would be lost.", "Delete", "Cancel");
        if (answer != 0)
        {
            return;
        }

        RunInBackground(
            app,
            live,
            $"Deleting {row.Session.Name}...",
            token => actions.DeleteAsync(row, forced: false, token),
            outcome =>
            {
                if (outcome.BranchToConfirm is null)
                {
                    return outcome.Message;
                }

                // Back on the UI thread, so the question is asked before the answer is used.
                int? answer = Confirm.Query(app, "Delete branch", $"Branch {outcome.BranchToConfirm.Branch} is not merged. Delete it anyway?", "Delete branch", "Keep");
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

    private void ResumeSessions(IApplication app, Live live)
    {
        // The checklist lists sessions in the order the table shows them, not the order the listing returned them.
        ResumePlan plan = SessionActions.PlanResume(live.State.VisibleRows);
        IReadOnlyList<SessionRow> chosen = plan.ToChoose.Count == 0 ? [] : ResumeChecklistDialog.Show(app, plan.ToChoose);
        List<SessionRow> toResume = [.. plan.Automatic, .. chosen];
        if (toResume.Count == 0)
        {
            live.State = live.State with { Footer = "Nothing to resume." };
            return;
        }

        RunInBackground(app, live, $"Resuming {toResume.Count}...", token => actions.ResumeAsync(toResume, token));
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

    private void RunWizard(IApplication app, Live live)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(app, launches, defaultBase);
        if (wizard is null)
        {
            return;
        }

        live.State = live.State with { Footer = $"Starting {wizard.Plan.Name.Text}..." };
        live.Redraw(clock.GetUtcNow());

        Task.Run(async () =>
        {
            string footer;
            ViewEffect? exit = null;
            try
            {
                LaunchOutcome outcome = await launches.LaunchAsync(wizard.Plan, CancellationToken.None);
                footer = outcome.Kind switch
                {
                    LaunchOutcomeKind.Started => $"Started {wizard.Plan.Name.Text} ({outcome.JobId}).",
                    LaunchOutcomeKind.WorkspaceNotTrusted => await launches.OpenTrustTabAsync(wizard.Plan.Directory, CancellationToken.None),
                    _ => $"Could not start: {outcome.Message}",
                };
                if (outcome.Kind == LaunchOutcomeKind.Started && wizard.AttachAfter && outcome.JobId is not null)
                {
                    exit = new AttachToSession(outcome.JobId);
                }
            }
            catch (Exception exception)
            {
                footer = $"Could not start: {exception.Message}";
            }

            app.Invoke(() =>
            {
                live.State = live.State with { Footer = footer };
                if (exit is not null)
                {
                    live.Exit = exit;
                    app.RequestStop();
                    return;
                }

                live.Redraw(clock.GetUtcNow());
                TriggerLoad(app, live);
            });
        });
    }

    /// <summary>The views alive during one Run, plus the state they show. Disposing it disposes the window and everything in it.</summary>
    private sealed class Live : IDisposable
    {
        private readonly TableView table;
        private readonly Label contextValues;
        private readonly string context;
        private readonly Label title;
        private readonly Label banner;
        private readonly Label footer;

        private Live(Window window, TableView table, Label contextValues, string context, Label title, Label banner, Label footer, SessionsViewState state)
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

        public SessionsViewState State { get; set; }

        public ViewEffect? Exit { get; set; }

        public static Live Build(IApplication app, SessionsViewState state, string context)
        {
            Window window = new() { BorderStyle = LineStyle.None };
            window.SetScheme(Palette.Plain(Palette.White));

            Label contextNames = Text(1, 0, 11, "Machine:\nClaude:\nRoots:\nSessions:\nNeed input:", Palette.Orange);
            Label contextValues = Text(13, 0, 48, context, Palette.White);
            Label keys = Text(62, 0, 10, "<enter>\n<o>\n<n>\n<r>\n<x>\n<d>", Palette.Blue);
            Label keyNames = Text(72, 0, 16, "Attach\nOpen in tab\nNew session\nRename\nStop\nDelete", Palette.White);
            Label keys2 = Text(90, 0, 8, "<R>\n<C>\n<a>\n<w>\n</>\n<?>\n<q>", Palette.Blue);
            Label keyNames2 = Text(98, 0, 20, "Resume stopped\nClose empty tabs\nAgent view\nWorktrees\nFilter\nHelp\nQuit", Palette.White);
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

            Label crumb = Text(1, Pos.AnchorEnd(2), 12, " <sessions> ", Palette.Background);
            crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
            Label footer = Text(14, Pos.AnchorEnd(2), Dim.Fill(), string.Empty, Palette.Cyan);

            window.Add(contextNames, contextValues, keys, keyNames, keys2, keyNames2, logo, banner, title, table, crumb, footer);

            Live live = new(window, table, contextValues, context, title, banner, footer, state);
            table.Style.RowColorGetter = live.RowColour;
            _ = app;

            return live;
        }

        public void Redraw(DateTimeOffset now)
        {
            IReadOnlyList<SessionRow> rows = State.VisibleRows;
            table.Table = new SessionTableSource(rows, now);
            contextValues.Text = $"{context}\n{Summary()}\n{State.NeedsInputCount}";
            title.Text = State.FilterEditing ? $"{State.Title}  filter: {State.Filter.Text}_" : State.Title;
            banner.Text = State.Banner is null ? string.Empty : $"  listing failed: {State.Banner}";
            footer.Text = State.Footer.Length == 0 ? $"{State.NeedsInputCount} need input" : State.Footer;
            int selected = Math.Max(rows.ToList().FindIndex(row => row.Session.SessionId == State.SelectedSessionId), 0);
            if (rows.Count > 0)
            {
                table.SetSelection(0, selected, false, null);
            }

            Window.SetNeedsDraw();
        }

        /// <summary>One line, 48 columns at most, so it never wraps into the "Need input" row (which has its own count).</summary>
        private string Summary()
        {
            const int width = 48;
            IEnumerable<string> byState = State.Rows
                .Where(row => row.Session.State.Kind != DisplayStateKind.NeedsInput)
                .GroupBy(row => row.Session.State.Label)
                .OrderByDescending(group => group.Count())
                .Select(group => $"{group.Count()} {group.Key}");
            string summary = State.Rows.Count == 0 ? "none" : $"{State.Rows.Count} ({string.Join(", ", byState)})";

            return summary.Length <= width ? summary : summary[..(width - 1)] + "…";
        }

        public void Dispose()
        {
            Window.Dispose();
        }

        private Scheme? RowColour(RowColorGetterArgs args)
        {
            if (args.Table is not SessionTableSource source)
            {
                return null;
            }

            SessionRow row = source.RowAt(args.RowIndex);

            return row.Session.SessionId == State.SelectedSessionId
                ? Palette.Highlight()
                : Palette.Plain(Palette.ForState(row.Session.State.Kind));
        }

        private static Label Text(Pos x, Pos y, Dim width, string text, Color colour)
        {
            Label label = new() { X = x, Y = y, Width = width, Height = Dim.Auto(), Text = text };
            label.SetScheme(Palette.Plain(colour));

            return label;
        }
    }
}