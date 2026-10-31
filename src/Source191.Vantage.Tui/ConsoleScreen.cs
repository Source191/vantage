using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The console: one Terminal.Gui application holding every tab. Row 0 is the settings line beside the icon; the Sessions,
/// Worktrees and Config tabs follow, the counts beside their titles; the key bar takes the last two rows. Keys go to the
/// active tab; a tab switch is applied by <see cref="ConsoleView"/> without leaving the application, and only the exit
/// ends the run (a session opens in a tab of its own, so nothing hands the terminal over), with every tab's state handed back.
/// </summary>
public sealed class ConsoleScreen(
    Func<CancellationToken, Task<ViewInput>> loadSessions,
    Func<CancellationToken, Task<ViewInput>> scanWorktrees,
    Func<CancellationToken, Task<ViewInput>> readConfig,
    Func<IReadOnlyList<ConfigRow>> settingRows,
    StateTransitionNotifier notifier,
    SessionActivity activity,
    DetailLoader details,
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
    private readonly Func<CancellationToken, Task<ViewInput>> readConfig = readConfig;
    private readonly Func<IReadOnlyList<ConfigRow>> settingRows = settingRows;
    private readonly StateTransitionNotifier notifier = notifier;
    private readonly SessionActivity activity = activity;
    private readonly DetailLoader details = details;
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
        private const int SettingsRows = 1;
        private const int StripTop = SettingsRows;
        private const int TitleRow = StripTop + 1;

        /// <summary>Where a status message's words start, after its mark or spinner and a space.</summary>
        private const int StatusWordsAt = 2;

        /// <summary>The cells kept after a message's last word for a space and its close mark.</summary>
        private const int StatusCloseRoom = 2;

        private readonly ConsoleScreen screen;
        private readonly IApplication app;
        private readonly ImageView icon;
        private readonly View[] settings;
        private readonly Tabs tabs;
        private readonly View counts;
        private readonly Label frameTitle;
        private readonly View bar;
        private readonly View statusRows;
        private readonly SpinnerView statusSpinner;
        private readonly Label statusMark;
        private readonly Label[] statusWords;
        private readonly Label statusClose;
        private readonly TableBody<SessionsViewState, SessionRow> sessions;
        private readonly TableBody<WorktreesViewState, WorktreeRow> worktrees;
        private readonly TableBody<ConfigViewState, ConfigRow> config;
        private readonly List<object> timers = [];
        private ConsoleTab active;
        private ViewEffect? leaving;
        private bool binding;
        private KeyBar? shownBar;
        private IReadOnlyList<TextPart> shownCounts = [];
        private IReadOnlyList<IReadOnlyList<TextPart>> shownSettings = [];
        private (Footer Footer, bool Running, int Width)? shownStatus;
        private int shownStatusRows;
        private int settingsIndent;
        private ITabBody? statusOwner;

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
            sessions = new TableBody<SessionsViewState, SessionRow>(new SessionsScreenDefinition(screen.notifier, screen.activity, screen.claudeExecutable, screen.folders, new SessionDetails(screen.details, screen.clock)), screen.dialogs, screen.dispatcher, screen.loadSessions, screen.clock, context, app, this, initial.Sessions);
            worktrees = new TableBody<WorktreesViewState, WorktreeRow>(new WorktreesScreenDefinition(screen.folders, new WorktreeDetails(screen.details, screen.clock)), screen.dialogs, screen.dispatcher, screen.scanWorktrees, screen.clock, context, app, this, initial.Worktrees);
            config = new TableBody<ConfigViewState, ConfigRow>(new ConfigScreenDefinition(), screen.dialogs, screen.dispatcher, screen.readConfig, screen.clock, context, app, this, initial.Config);
            tabs.Add(sessions.View, worktrees.View, config.View);
            Array.ForEach([sessions.View, worktrees.View, config.View], view => view.SetScheme(Palette.TabFrame()));
            ReleaseNavigationKeys(tabs, sessions.View, worktrees.View, config.View);
            counts = new View { X = HeaderSpan() + 2, Y = TitleRow, Width = Dim.Fill(), Height = 1 };
            frameTitle = new Label { X = 0, Y = TitleRow, Width = 0, Height = 1, HotKeySpecifier = new System.Text.Rune(0xFFFF), Text = string.Empty };
            bar = new View { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Height = 2 };
            // The status line: under the settings row and over the tabs, outside every tab, with rows only while the shown tab has something to say.
            statusRows = new View { X = 0, Y = SettingsRows, Width = Dim.Fill(), Height = 0, Visible = false };
            statusSpinner = Spinners.Create(0, 0);
            statusMark = Text(0, 0, string.Empty, Palette.White);
            statusMark.Width = 1;
            statusWords = [.. Enumerable.Range(0, StatusLayout.MostRows).Select(row => Text(StatusWordsAt, row, string.Empty, Palette.White))];
            Array.ForEach(statusWords, label => label.Width = Dim.Fill(StatusCloseRoom));
            statusClose = Text(0, 0, Wording.StatusClose, Palette.Blue);
            statusClose.Visible = false;
            statusClose.MouseEvent += (_, mouse) => CloseStatus(mouse);
            app.ToolTips?.SetToolTip(statusClose, () => ToolTips.View(Wording.StatusCloseTip));
            View[] statusParts = [statusSpinner, statusMark, .. statusWords, statusClose];
            statusRows.Add(statusParts);
            View[] views = [icon, .. settings, statusRows, tabs, counts, frameTitle, bar];
            Window.Add(views);

            Window.KeyDown += (_, key) => Active.OnKey(key);
            tabs.ValueChanged += (_, _) => OnTabClicked();
            Window.Initialized += (_, _) => Show(active);

            Start(sessions, always: true);
            Start(worktrees, always: false);
            sessions.Redraw();
            worktrees.Redraw();
            config.Redraw();
        }

        public Window Window { get; }

        public ConsoleExit Exit => new(new ConsoleState(sessions.State, worktrees.State, config.State, active), leaving);

        private ITabBody Active => BodyOf(active);

        private ITabBody BodyOf(ConsoleTab tab) => tab switch
        {
            ConsoleTab.Sessions => sessions,
            ConsoleTab.Worktrees => worktrees,
            ConsoleTab.Config => config,
        };

        public void Leave(ViewEffect effect)
        {
            leaving = effect;
            app.RequestStop();
        }

        public void Switch(ISwitchesTab effect)
        {
            ConsoleState state = ConsoleView.Apply(new ConsoleState(sessions.State, worktrees.State, config.State, active), effect);
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
            ShowStatus(Active.Status, Active.Running);
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

        /// <summary>Takes the tabs out of the strip and disposes them, then the window and the rest of its views.</summary>
        public void Dispose()
        {
            // Removing a tab selects the next in the strip; under the guard that is not taken as a click, which would show it and read its detail.
            binding = true;
            tabs.Remove(sessions.View);
            tabs.Remove(worktrees.View);
            tabs.Remove(config.View);
            sessions.Dispose();
            worktrees.Dispose();
            config.Dispose();
            Window.Dispose();
        }

        private int ScreenWidth => Window.Viewport.Width > 0 ? Window.Viewport.Width : app.Screen.Width;

        /// <summary>
        /// Shows a tab: selects it in the strip (ignoring the change event that raises), records it, starts the worktrees scan
        /// the first time it is shown, and tells both tabs which is shown so the hidden one keeps its spinners still.
        /// </summary>
        private void Show(ConsoleTab tab)
        {
            active = tab;
            binding = true;
            tabs.Value = BodyOf(tab).View;
            binding = false;
            if (tab != screen.shown)
            {
                screen.shown = tab;
                screen.context.Diagnostics.Record(new ViewShown(tab switch
                {
                    ConsoleTab.Sessions => ViewKind.Sessions,
                    ConsoleTab.Worktrees => ViewKind.Worktrees,
                    ConsoleTab.Config => ViewKind.Config,
                }));
            }

            if (tab == ConsoleTab.Worktrees && !screen.worktreesScanned)
            {
                screen.worktreesScanned = true;
                worktrees.TriggerLoad();
            }

            if (tab == ConsoleTab.Config)
            {
                ReadConfig();
            }

            sessions.Shown(tab == ConsoleTab.Sessions);
            worktrees.Shown(tab == ConsoleTab.Worktrees);
            config.Shown(tab == ConsoleTab.Config);
        }

        /// <summary>The setting rows at once the first time, so they never wait for the setup, which is read off the UI thread each time the tab is shown.</summary>
        private void ReadConfig()
        {
            if (!config.State.Loaded)
            {
                config.Reduce(new ConfigRowsLoaded(screen.settingRows()));
            }

            config.TriggerLoad();
        }

        /// <summary>
        /// `Tabs` binds the arrow and paging keys to moving between tabs, so Up and Down switched tabs instead of moving
        /// the selection; without those bindings the keys reach the window and go to the active tab's reducer.
        /// </summary>
        private static void ReleaseNavigationKeys(params View[] views)
        {
            Terminal.Gui.Input.Key[] keys =
            [
                Terminal.Gui.Input.Key.CursorUp, Terminal.Gui.Input.Key.CursorDown, Terminal.Gui.Input.Key.CursorLeft, Terminal.Gui.Input.Key.CursorRight,
                Terminal.Gui.Input.Key.PageUp, Terminal.Gui.Input.Key.PageDown, Terminal.Gui.Input.Key.Home, Terminal.Gui.Input.Key.End,
            ];
            foreach (View view in views)
            {
                foreach (Terminal.Gui.Input.Key key in keys)
                {
                    view.KeyBindings.Remove(key);
                }
            }
        }

        /// <summary>A click on a tab's title selects it in the strip; the console follows as if the tab's key had been pressed.</summary>
        private void OnTabClicked()
        {
            if (binding)
            {
                return;
            }

            ConsoleTab clicked = Enum.GetValues<ConsoleTab>().First(tab => BodyOf(tab).View == tabs.Value);
            if (clicked != active)
            {
                Show(clicked);
            }
        }

        /// <summary>The icon shows where the terminal draws images, and the two settings rows start after it; elsewhere they start at the left edge.</summary>
        private void ShowSettings()
        {
            int indent = VantageIcon.Fit(icon, app.Driver?.SixelSupport);
            // The status line's mark lines up with the first letter of the settings row.
            settingsIndent = indent;
            statusRows.X = indent;
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

        /// <summary>The table's title at the right end of the tab titles' row, a cell from the edge; cut at its end with an ellipsis when it would reach the counts.</summary>
        private void ShowFrameTitle(TextPart title)
        {
            int countsEnd = HeaderSpan() + 2 + string.Join(Separator, shownCounts.Select(part => part.Text)).Length;
            int room = Math.Max(ScreenWidth - 1 - (countsEnd + 2), 0);
            string text = title.Text.Length <= room ? title.Text : title.Text[..Math.Max(room - 1, 0)] + "…";
            frameTitle.X = Math.Max(ScreenWidth - 1 - text.Length, 0);
            frameTitle.Width = text.Length;
            frameTitle.Text = text;
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
        private int HeaderSpan() => sessions.View.Title.Length + worktrees.View.Title.Length + config.View.Title.Length + 4;

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

        /// <summary>
        /// The shown tab's status line under the settings row: the spinner while its action runs, otherwise its tone's glyph,
        /// then the words in the tone's colour, wrapped onto at most three rows. While it shows, the tab strip, its counts and
        /// the table's title move down by its rows; the tabs keep their place above the key bar.
        /// </summary>
        private void ShowStatus(Footer footer, bool running)
        {
            int width = Math.Max(ScreenWidth - settingsIndent - StatusWordsAt - StatusCloseRoom, 1);
            // By reference: a new message with the same words as the last is still a new message, with its own timer.
            bool newMessage = shownStatus is not { } shown || !ReferenceEquals(shown.Footer, footer);
            if (!newMessage && shownStatus is { } same && same.Running == running && same.Width == width)
            {
                return;
            }

            shownStatus = (footer, running, width);
            statusOwner = Active;
            IReadOnlyList<string> lines = StatusLayout.Lines(Wording.Describe(footer), width);
            StatusTone tone = running ? StatusTone.Working : Wording.ToneOf(footer);
            TextPart mark = Wording.StatusMark(tone);
            // The glyph and the spinner share a cell; the glyph, drawn after it, would blank the spinner.
            statusSpinner.Show(running);
            statusMark.Visible = !running;
            statusMark.Text = mark.Text;
            statusMark.SetScheme(Palette.Plain(Palette.ForTone(mark.Tone)));
            Color words = Palette.ForTone(Wording.StatusWordsTone(tone));
            for (int row = 0; row < statusWords.Length; row++)
            {
                statusWords[row].Text = row < lines.Count ? lines[row] : string.Empty;
                statusWords[row].SetScheme(Palette.Plain(words));
            }

            ShowClose(footer, running, lines);
            if (newMessage && !running && StatusLayout.ClearsAfter(tone) is TimeSpan after)
            {
                ClearLater(Active, footer, after);
            }

            LayOutStatus(running ? Math.Max(lines.Count, 1) : lines.Count);
        }

        /// <summary>The close mark after the message's last word, on its last row; none while an action runs or for a standing description.</summary>
        private void ShowClose(Footer footer, bool running, IReadOnlyList<string> lines)
        {
            statusClose.Visible = lines.Count > 0 && StatusLayout.Closes(footer, running);
            if (!statusClose.Visible)
            {
                return;
            }

            statusClose.X = StatusWordsAt + lines[^1].Length + 1;
            statusClose.Y = lines.Count - 1;
        }

        /// <summary>A click on the close mark clears the message it was drawn for, in the tab that showed it.</summary>
        private void CloseStatus(Terminal.Gui.Input.Mouse mouse)
        {
            if (!mouse.Flags.HasFlag(Terminal.Gui.Input.MouseFlags.LeftButtonClicked) || statusOwner is null || shownStatus is not { } shown)
            {
                return;
            }

            mouse.Handled = true;
            // The pointer has not moved, so the tooltip would stay, drawn over the tabs as they move up into the line's place.
            app.ToolTips?.Hide();
            statusOwner.Reduce(new MessageDismissed(shown.Footer));
        }

        /// <summary>Clears a success once its time is up, in the tab that showed it; a newer message there is left alone.</summary>
        private void ClearLater(ITabBody owner, Footer footer, TimeSpan after)
        {
            object? timer = null;
            timer = app.AddTimeout(after, () =>
            {
                timers.Remove(timer!);
                owner.Reduce(new MessageDismissed(footer));
                return false;
            });
            if (timer is not null)
            {
                timers.Add(timer);
            }
        }

        /// <summary>Gives the status line its rows and moves the tab strip, its counts and the table's title down by them; nothing moves when the count is the same.</summary>
        private void LayOutStatus(int rows)
        {
            if (rows == shownStatusRows)
            {
                return;
            }

            shownStatusRows = rows;
            statusRows.Visible = rows > 0;
            statusRows.Height = rows;
            tabs.Y = StripTop + rows;
            counts.Y = TitleRow + rows;
            frameTitle.Y = TitleRow + rows;
            Window.SetNeedsLayout();
        }
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
                Label[] pair = [key, word];
                foreach (Label label in pair)
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