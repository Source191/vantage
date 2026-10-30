using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The console: one Terminal.Gui application holding both tabs. Row 0 is the settings line; the Sessions and Worktrees
/// tabs follow, the counts beside their titles; the key bar takes the last two rows. Keys go to the active tab; a tab
/// switch is applied by <see cref="ConsoleView"/> without leaving the application, and only the exit or an effect that
/// needs the terminal (attach) ends a run, with both tabs' state handed back.
/// </summary>
public sealed class ConsoleScreen(
    Func<CancellationToken, Task<ViewInput>> loadSessions,
    Func<CancellationToken, Task<ViewInput>> scanWorktrees,
    StateTransitionNotifier notifier,
    DialogEffects dialogs,
    EffectDispatcher dispatcher,
    TimeProvider clock,
    ScreenContext context,
    string claudeExecutable,
    FolderLabels folders)
    : IConsoleScreen
{
    private readonly Func<CancellationToken, Task<ViewInput>> loadSessions = loadSessions;
    private readonly Func<CancellationToken, Task<ViewInput>> scanWorktrees = scanWorktrees;
    private readonly StateTransitionNotifier notifier = notifier;
    private readonly DialogEffects dialogs = dialogs;
    private readonly EffectDispatcher dispatcher = dispatcher;
    private readonly TimeProvider clock = clock;
    private readonly ScreenContext context = context;
    private readonly string claudeExecutable = claudeExecutable;
    private readonly FolderLabels folders = folders;
    private ConsoleTab? shown;
    private bool worktreesScanned;

    public ConsoleExit Run(ConsoleState initial)
    {
        using IApplication app = Terminal.Gui.App.Application.Create().Init();
        // Windows Terminal renders 24-bit colour; without this the driver falls back to 16 colours and the palette shifts.
        if (app.Driver is not null)
        {
            app.Driver.Force16Colors = false;
        }

        using Frame frame = new(this, app, initial);
        context.Clipboard.Attach(app);
        try
        {
            app.Run(frame.Window);
        }
        finally
        {
            context.Clipboard.Detach();
            frame.StopTimers();
        }

        return frame.Exit;
    }

    /// <summary>One run's views and state: the window, the icon and settings rows, the two tabs, the counts and title beside them, the key bar and the timers.</summary>
    private sealed class Frame : ITabHost, IDisposable
    {
        private const string Separator = " · ";
        private const int SettingsRows = 2;
        private const int StripTop = SettingsRows;
        private const int TitleRow = StripTop + 1;
        private const int FrameRow = StripTop + 2;

        private readonly ConsoleScreen screen;
        private readonly IApplication app;
        private readonly ImageView icon;
        private readonly View[] settings;
        private readonly Tabs tabs;
        private readonly View counts;
        private readonly Label frameTitle;
        private readonly View bar;
        private readonly TableBody<SessionsViewState, SessionRow> sessions;
        private readonly TableBody<WorktreesViewState, WorktreeRow> worktrees;
        private readonly List<object> timers = [];
        private ConsoleTab active;
        private ViewEffect? leaving;
        private bool binding;
        private KeyBar? shownBar;
        private IReadOnlyList<TextPart> shownCounts = [];
        private IReadOnlyList<IReadOnlyList<TextPart>> shownSettings = [];

        public Frame(ConsoleScreen screen, IApplication app, ConsoleState initial)
        {
            this.screen = screen;
            this.app = app;
            active = initial.Active;
            ScreenContext context = screen.context;

            // Terminal.Gui writes the top-level's Title to the terminal (OSC 0) on every run, so this is what the tab shows.
            Window = new Window { BorderStyle = LineStyle.None, Title = context.Title };
            Window.SetScheme(Palette.Plain(Palette.White));

            icon = VantageIcon.Create();
            settings = [.. Enumerable.Range(0, SettingsRows).Select(row => new View { X = 0, Y = row, Width = Dim.Fill(), Height = 1 })];
            tabs = new Tabs { X = 0, Y = StripTop, Width = Dim.Fill(), Height = Dim.Fill(2) };
            tabs.SetScheme(Palette.Plain(Palette.Grey));
            sessions = new TableBody<SessionsViewState, SessionRow>(new SessionsScreenDefinition(screen.notifier, screen.claudeExecutable, screen.folders), screen.dialogs, screen.dispatcher, screen.loadSessions, screen.clock, context, app, this, initial.Sessions);
            worktrees = new TableBody<WorktreesViewState, WorktreeRow>(new WorktreesScreenDefinition(screen.folders), screen.dialogs, screen.dispatcher, screen.scanWorktrees, screen.clock, context, app, this, initial.Worktrees);
            tabs.Add(sessions.View, worktrees.View);
            counts = new View { X = HeaderSpan() + 2, Y = TitleRow, Width = Dim.Fill(), Height = 1 };
            frameTitle = new Label { X = HeaderSpan() + 1, Y = FrameRow, Width = Dim.Auto(), Height = 1, HotKeySpecifier = new System.Text.Rune(0xFFFF), Text = string.Empty };
            bar = new View { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Height = 2 };
            View[] views = [icon, .. settings, tabs, counts, frameTitle, bar];
            Window.Add(views);

            Window.KeyDown += (_, key) => Active.OnKey(key);
            tabs.ValueChanged += (_, _) => OnTabClicked();
            Window.Initialized += (_, _) => Show(active);

            Start(sessions, always: true);
            Start(worktrees, always: false);
            sessions.Redraw();
            worktrees.Redraw();
        }

        public Window Window { get; }

        public ConsoleExit Exit => new(new ConsoleState(sessions.State, worktrees.State, active), leaving);

        private ITabBody Active => active switch
        {
            ConsoleTab.Sessions => sessions,
            ConsoleTab.Worktrees => worktrees,
        };

        public void Leave(ViewEffect effect)
        {
            leaving = effect;
            app.RequestStop();
        }

        public void Switch(ISwitchesTab effect)
        {
            ConsoleState state = ConsoleView.Apply(new ConsoleState(sessions.State, worktrees.State, active), effect);
            if (state.Sessions != sessions.State)
            {
                sessions.Reduce(new SelectSession(((JumpToSession)effect).SessionId));
            }

            Show(state.Active);
        }

        public void FrameChanged()
        {
            ShowSettings();
            ShowCounts(Active.Counts);
            ShowFrameTitle(Active.FrameTitle);
            ShowBar(Active.Bar(screen.context.UpdateKnown()));
            Window.SetNeedsDraw();
        }

        public void StopTimers()
        {
            foreach (object timer in timers)
            {
                app.RemoveTimeout(timer);
            }

            timers.Clear();
        }

        /// <summary>Takes the two tabs out of the strip and disposes them, then the window and the rest of its views.</summary>
        public void Dispose()
        {
            tabs.Remove(sessions.View);
            tabs.Remove(worktrees.View);
            sessions.Dispose();
            worktrees.Dispose();
            Window.Dispose();
        }

        private int ScreenWidth => Window.Viewport.Width > 0 ? Window.Viewport.Width : app.Screen.Width;

        /// <summary>
        /// Shows a tab: selects it in the strip (ignoring the change event that raises), draws its title and frame in cyan
        /// and the other's title in grey, records it, and starts the worktrees scan the first time it is shown.
        /// </summary>
        private void Show(ConsoleTab tab)
        {
            active = tab;
            binding = true;
            tabs.Value = tab == ConsoleTab.Sessions ? sessions.View : worktrees.View;
            binding = false;
            sessions.View.SetScheme(Palette.Plain(tab == ConsoleTab.Sessions ? Palette.Cyan : Palette.Grey));
            worktrees.View.SetScheme(Palette.Plain(tab == ConsoleTab.Worktrees ? Palette.Cyan : Palette.Grey));
            if (tab != screen.shown)
            {
                screen.shown = tab;
                screen.context.Diagnostics.Record(new ViewShown(tab switch
                {
                    ConsoleTab.Sessions => ViewKind.Sessions,
                    ConsoleTab.Worktrees => ViewKind.Worktrees,
                }));
            }

            if (tab == ConsoleTab.Worktrees && !screen.worktreesScanned)
            {
                screen.worktreesScanned = true;
                worktrees.TriggerLoad();
            }

            Active.Redraw();
        }

        /// <summary>A click on a tab's title selects it in the strip; the console follows as if the tab's key had been pressed.</summary>
        private void OnTabClicked()
        {
            if (binding)
            {
                return;
            }

            ConsoleTab clicked = tabs.Value == worktrees.View ? ConsoleTab.Worktrees : ConsoleTab.Sessions;
            if (clicked != active)
            {
                Show(clicked);
            }
        }

        /// <summary>The icon shows where the terminal draws images, and the two settings rows start after it; elsewhere they start at the left edge.</summary>
        private void ShowSettings()
        {
            icon.Visible = app.Driver?.SixelSupport?.IsSupported == true;
            int indent = icon.Visible ? VantageIcon.Indent : 0;
            IReadOnlyList<IReadOnlyList<TextPart>> rows = screen.context.SettingsRows(ScreenWidth - indent);
            if (settings[0].X.Equals(Pos.Absolute(indent)) && rows.Count == shownSettings.Count && rows.Zip(shownSettings).All(pair => pair.First.SequenceEqual(pair.Second)))
            {
                return;
            }

            shownSettings = rows;
            for (int row = 0; row < settings.Length; row++)
            {
                settings[row].X = indent;
                Fill(settings[row], row < rows.Count ? rows[row] : [], separator: null);
            }
        }

        /// <summary>The table's title written into the frame's line after the tabs, a space either side, cut to stop short of the frame's corner.</summary>
        private void ShowFrameTitle(TextPart title)
        {
            int room = Math.Max(ScreenWidth - HeaderSpan() - 3, 0);
            string text = $" {title.Text} ";
            frameTitle.Text = text.Length <= room ? text : text[..room];
            frameTitle.SetScheme(Palette.Plain(Palette.ForTone(title.Tone)));
        }

        /// <summary>The counts after the tab strip's titles, each in its tone and joined by a grey dot; rebuilt only when they change.</summary>
        private void ShowCounts(IReadOnlyList<TextPart> parts)
        {
            if (parts.SequenceEqual(shownCounts))
            {
                return;
            }

            shownCounts = parts;
            Fill(counts, parts, new TextPart(Separator, TextTone.Grey));
        }

        /// <summary>Replaces a row's labels with one per part, in its tone, with the separator between parts when one is given.</summary>
        private static void Fill(View row, IReadOnlyList<TextPart> parts, TextPart? separator)
        {
            row.RemoveAll();
            int x = 0;
            for (int index = 0; index < parts.Count; index++)
            {
                if (index > 0 && separator is not null)
                {
                    row.Add(Text(x, 0, separator.Text, Palette.ForTone(separator.Tone)));
                    x += separator.Text.Length;
                }

                row.Add(Text(x, 0, parts[index].Text, Palette.ForTone(parts[index].Tone)));
                x += parts[index].Text.Length;
            }
        }

        /// <summary>The strip's width: each title with a border cell either side, adjacent tabs sharing one (measured with the spike of 30 October 2026); the titles never change, so neither does this.</summary>
        private int HeaderSpan() => sessions.View.Title.Length + worktrees.View.Title.Length + 3;

        /// <summary>The sessions tab refreshes always; the worktrees tab only while it is shown, once its first scan has been asked for.</summary>
        private void Start(ITabBody body, bool always)
        {
            object? timer = app.AddTimeout(body.RefreshEvery, () =>
            {
                if (always || (body == Active && screen.worktreesScanned))
                {
                    body.TriggerLoad();
                }

                return true;
            });
            if (timer is not null)
            {
                timers.Add(timer);
            }

            if (always)
            {
                body.TriggerLoad();
            }
        }

        /// <summary>The counts after the tab strip's titles, each in its tone and joined by a grey dot; rebuilt when they or the titles change.</summary>
        /// <summary>The two rows of the key bar: each item a blue key and a white word, both answering a click with the key and a hover with its tip; rebuilt only when the items change.</summary>
        private void ShowBar(KeyBar keys)
        {
            if (shownBar is not null && keys.First.SequenceEqual(shownBar.First) && keys.Second.SequenceEqual(shownBar.Second))
            {
                return;
            }

            shownBar = keys;
            bar.RemoveAll();
            AddBarRow(keys.First, 0);
            AddBarRow(keys.Second, 1);
        }

        private void AddBarRow(IReadOnlyList<KeyBarItem> items, int row)
        {
            foreach (KeyBarItem item in items)
            {
                Label key = Text(item.X, row, item.Key, Palette.Blue);
                Label word = Text(item.X + item.Key.Length + 1, row, item.Word, Palette.White);
                foreach (Label label in new[] { key, word })
                {
                    label.MouseEvent += (_, mouse) => Click(mouse, item);
                    app.ToolTips?.SetToolTip(label, () => ToolTips.View(item.Tip));
                }

                bar.Add(key, word);
            }
        }

        private void Click(Terminal.Gui.Input.Mouse mouse, KeyBarItem item)
        {
            if (!mouse.Flags.HasFlag(Terminal.Gui.Input.MouseFlags.LeftButtonClicked))
            {
                return;
            }

            mouse.Handled = true;
            Active.Press(item.Key switch
            {
                "Enter" => Key.Enter,
                "Esc" => Key.Escape,
                _ => Key.Of(item.Key[0]),
            });
        }

        /// <summary>A label that shows its text as written: no character is taken as a hotkey mark.</summary>
        private static Label Text(int x, int y, string text, Color colour)
        {
            Label label = new() { X = x, Y = y, Width = text.Length, Height = 1, HotKeySpecifier = new System.Text.Rune(0xFFFF), Text = text };
            label.SetScheme(Palette.Plain(colour));

            return label;
        }
    }
}