using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Alignment = Terminal.Gui.ViewBase.Alignment;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>What a screen hands back when it stops: the state to resume with and the effect that needs the caller.</summary>
internal sealed record ScreenRun<TState>(TState State, ViewEffect? Exit);

/// <summary>
/// The shared table screen: one Terminal.Gui application per run, a refresh timer, keys to the reducer, effects routed
/// by what they need (leaving the screen returns them to the caller, a dialog goes to <see cref="DialogEffects"/>, the
/// rest to their handler through the <see cref="EffectDispatcher"/> with a busy spinner), a waiting indicator until the
/// first load, and the header, table, crumb and footer layout. A definition supplies everything specific to a view.
/// </summary>
internal sealed class TableScreen<TState, TRow>(
    IScreenDefinition<TState, TRow> definition,
    DialogEffects dialogs,
    EffectDispatcher dispatcher,
    Func<CancellationToken, Task<ViewInput>> load,
    TimeProvider clock,
    ScreenContext context)
    where TState : IListViewState<TRow>
{
    private readonly IScreenDefinition<TState, TRow> definition = definition;
    private readonly DialogEffects dialogs = dialogs;
    private readonly EffectDispatcher dispatcher = dispatcher;
    private readonly Func<CancellationToken, Task<ViewInput>> load = load;
    private readonly TimeProvider clock = clock;
    private readonly ScreenContext context = context;

    public ScreenRun<TState> Run(TState initial)
    {
        using IApplication app = Terminal.Gui.App.Application.Create().Init();
        // Windows Terminal renders 24-bit colour; without this the driver falls back to 16 colours and the palette shifts.
        if (app.Driver is not null)
        {
            app.Driver.Force16Colors = false;
        }

        using Live live = new(definition, context, app);
        live.ShowToolTips();
        Running running = new(this, app, live, initial);
        live.Window.KeyDown += (_, key) => running.OnKey(key);
        live.Pressed = running.Press;
        running.Redraw();

        object? timer = app.AddTimeout(definition.RefreshEvery, () =>
        {
            running.TriggerLoad();
            return true;
        });
        running.TriggerLoad();

        context.Clipboard.Attach(app);
        try
        {
            app.Run(live.Window);
        }
        finally
        {
            context.Clipboard.Detach();
        }

        if (timer is not null)
        {
            app.RemoveTimeout(timer);
        }

        return new ScreenRun<TState>(running.State, running.Exit);
    }

    /// <summary>One run's mutable state: what the reducer last produced and the effect that ended the run.</summary>
    private sealed class Running(TableScreen<TState, TRow> screen, IApplication app, Live live, TState initial) : IEffectContext
    {
        public IApplication App => app;

        public DiagnosticLog Diagnostics => screen.context.Diagnostics;

        public string HelpText => screen.definition.HelpText;

        public TState State { get; private set; } = initial;

        public ViewEffect? Exit { get; private set; }

        public void TriggerLoad()
        {
            Task.Run(async () =>
            {
                ViewInput input = await Attempt.RunAsync(Diagnostics, FailureSite.Load, CodeName.For(typeof(TState)), () => screen.load(CancellationToken.None), exception => new LoadFailed(exception.Message));

                app.Invoke(() => Apply(input));
            });
        }

        public void OnKey(GuiKey key)
        {
            Key? mapped = KeyMapping.From(key);
            if (mapped is null)
            {
                return;
            }

            key.Handled = true;
            Press(mapped);
        }

        /// <summary>A key from the keyboard or a click on the key bar: Left and Right scroll the table, and every key goes to the reducer.</summary>
        public void Press(Key key)
        {
            live.Scroll(key);
            Apply(new KeyPressed(key));
        }

        public void Reduce(ViewInput input) => Apply(input);

        public void Redraw() => live.Redraw(State, screen.clock.GetUtcNow());

        public void ShowFooter(Footer footer) => State = screen.definition.WithFooter(State, footer);

        public void Dispatch(ViewEffect effect) => Dispatch(effect, outcome => new Footer.Reported(outcome));

        public void Dispatch(ViewEffect effect, Func<Outcome, Footer> onUiThread) => RunInBackground(new Footer.Working(effect), token => screen.dispatcher.DispatchAsync(effect, token), onUiThread);

        public void RunInBackground<T>(Footer working, Func<CancellationToken, Task<T>> action, Func<T, Footer> onUiThread)
        {
            State = screen.definition.WithFooter(State, working);
            Redraw();
            live.ShowBusy(true);
            Task.Run(async () =>
            {
                // The continuation is built here but run on the UI thread, where onUiThread must run.
                Func<Footer> settle = await Attempt.RunAsync<Func<Footer>>(
                    Diagnostics,
                    FailureSite.Effect,
                    Attempt.What(working),
                    async () =>
                    {
                        T result = await action(CancellationToken.None);
                        return () => onUiThread(result);
                    },
                    exception => () => new Footer.Failed(exception.Message));

                app.Invoke(() =>
                {
                    live.ShowBusy(false);
                    State = screen.definition.WithFooter(State, settle());
                    Redraw();
                    TriggerLoad();
                });
            });
        }

        private void Apply(ViewInput input)
        {
            screen.definition.OnInput(State, input);
            (TState state, IReadOnlyList<ViewEffect> effects) = screen.definition.Reduce(State, input);
            State = state;
            foreach (ViewEffect effect in effects)
            {
                switch (effect)
                {
                    case ILeavesScreen or ISwitchesTab:
                        Exit = effect;
                        app.RequestStop();
                        return;
                    case IDialogEffect:
                        screen.dialogs.Show(this, effect);
                        break;
                    default:
                        Dispatch(effect);
                        break;
                }
            }

            Redraw();
        }
    }

    /// <summary>The views alive during one run. Disposing it disposes the window and everything in it.</summary>
    private sealed class Live : IDisposable
    {
        private const int LongColumnCap = 28;
        private const int ElasticFloor = 20;
        private const string CountSeparator = " · ";

        private readonly IScreenDefinition<TState, TRow> definition;
        private readonly ScreenContext context;
        private readonly IApplication app;
        private readonly TableView table;
        private readonly Label settingsLine;
        private readonly Label tabTitle;
        private readonly View counts;
        private readonly Label title;
        private readonly Label banner;
        private readonly Label footer;
        private readonly View bar;
        private readonly SpinnerView busy;
        private readonly WaitingIndicator waiting;
        private TRow? selected;
        private readonly string[] columnNames;
        private int? headerUnderMouse;
        private int columnOffset;
        private KeyBar? shownBar;
        private IReadOnlyList<CountPart> shownCounts = [];

        public Live(IScreenDefinition<TState, TRow> definition, ScreenContext context, IApplication app)
        {
            this.definition = definition;
            this.context = context;
            this.app = app;
            columnNames = [.. definition.Columns.Select(column => column.Name)];

            // Terminal.Gui writes the top-level's Title to the terminal (OSC 0) on every run, so this is what the tab shows.
            Window = new Window { BorderStyle = LineStyle.None, Title = context.Title };
            Window.SetScheme(Palette.Plain(Palette.White));

            settingsLine = Text(0, 0, Dim.Fill(), string.Empty, Palette.Grey);
            tabTitle = Text(1, 1, Dim.Auto(), string.Empty, Palette.Background);
            tabTitle.SetScheme(Palette.Pair(Palette.Background, Palette.Cyan));
            counts = new View { X = Pos.Right(tabTitle) + 2, Y = 1, Width = Dim.Fill(), Height = 1 };
            banner = Text(1, 2, Dim.Fill(), string.Empty, Palette.Red);
            title = Text(2, 2, Dim.Fill(), string.Empty, Palette.Cyan);

            table = new TableView
            {
                X = 1,
                Y = 3,
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
            table.Style.RowColorGetter = RowColour;
            StyleColumns();

            int crumbWidth = definition.Crumb.Length;
            Label crumb = Text(1, Pos.AnchorEnd(3), crumbWidth, definition.Crumb, Palette.Background);
            crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
            busy = Spinners.Create(crumbWidth + 2, Pos.AnchorEnd(3));
            waiting = new WaitingIndicator(definition.WaitingText);
            footer = Text(crumbWidth + 4, Pos.AnchorEnd(3), Dim.Fill(), string.Empty, Palette.Cyan);
            bar = new View { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Height = 2 };

            Window.Add(settingsLine, tabTitle, counts, banner, title, table, crumb, busy, footer, bar, waiting);
        }

        public Window Window { get; }

        /// <summary>What a click on a key bar item does: the same as pressing its key.</summary>
        public Action<Key> Pressed { get; set; } = _ => { };

        /// <summary>Left and Right move the table one column sideways, within its columns; presentation only, so the reducer never sees a change.</summary>
        public void Scroll(Key key)
        {
            int step = key.Name switch
            {
                KeyName.Left => -1,
                KeyName.Right => 1,
                _ => 0,
            };
            if (step == 0)
            {
                return;
            }

            // Kept here as well as on the table, because every redraw replaces the table's source, which resets its offset.
            columnOffset = Math.Clamp(columnOffset + step, 0, columnNames.Length - 1);
            table.ColumnOffset = columnOffset;
            table.SetNeedsDraw();
        }

        /// <summary>The footer spinner: on while an action runs off the UI thread, off once its outcome is shown.</summary>
        public void ShowBusy(bool working) => busy.Show(working);

        public void Redraw(TState state, DateTimeOffset now)
        {
            IReadOnlyList<TRow> rows = state.List.Visible;
            selected = state.List.Selected;
            int elasticWidth = ElasticWidth(rows, now);
            table.Table = new TableSource<TRow>(rows, columnNames, (row, column) => definition.Cell(row, column, now, column == definition.ElasticColumn ? elasticWidth : int.MaxValue));
            table.ColumnOffset = columnOffset;
            settingsLine.Text = context.SettingsLine(ScreenWidth);
            tabTitle.Text = $" {definition.TabTitle(state)} ";
            ShowCounts(definition.Counts(state));
            string described = Wording.Describe(state.Title);
            title.Text = state.List.FilterEditing ? $"{described}  filter: {state.List.Filter.Text}_" : described;
            banner.Text = state.Banner is null ? string.Empty : $"  {definition.LoadFailurePrefix}: {state.Banner}";
            title.Visible = state.Banner is null;
            footer.Text = Wording.Describe(state.Shown);
            ShowBar(definition.Bar(state, context.UpdateKnown()));
            if (rows.Count > 0)
            {
                int index = selected is null ? 0 : Math.Max(rows.ToList().IndexOf(selected), 0);
                table.SetSelection(0, index, false, null);
            }

            waiting.Show(!state.Loaded && state.Banner is null);
            Window.SetNeedsDraw();
        }

        /// <summary>Shows a column's meaning as a tooltip while the mouse rests on the column's header; the key bar's items carry their own.</summary>
        public void ShowToolTips()
        {
            table.MousePositionTracking = true;
            table.MouseEvent += (_, mouse) => ShowColumnToolTip(mouse);
            table.MouseLeave += (_, _) => HideColumnToolTip();
        }

        public void Dispose() => Window.Dispose();

        private int ScreenWidth => Window.Viewport.Width > 0 ? Window.Viewport.Width : app.Screen.Width;

        private void HideColumnToolTip()
        {
            if (headerUnderMouse is not null)
            {
                app.ToolTips?.Hide();
            }

            headerUnderMouse = null;
        }

        private void ShowColumnToolTip(Terminal.Gui.Input.Mouse mouse)
        {
            int? header = null;
            if (mouse.Position is System.Drawing.Point position)
            {
                table.ScreenToCell(position, out header);
            }

            if (header == headerUnderMouse)
            {
                return;
            }

            HideColumnToolTip();
            headerUnderMouse = header;
            if (header is int column && column < definition.Columns.Count)
            {
                ColumnHelp help = definition.Columns[column];
                app.ToolTips?.ShowFor(table, new ToolTipProvider(() => ToolTipView($"{help.Name}: {help.Meaning}")));
            }
        }

        /// <summary>The counts beside the tab's title, each in its tone, joined by a grey dot; rebuilt only when they change.</summary>
        private void ShowCounts(IReadOnlyList<CountPart> parts)
        {
            if (parts.SequenceEqual(shownCounts))
            {
                return;
            }

            shownCounts = parts;
            counts.RemoveAll();
            int x = 0;
            for (int index = 0; index < parts.Count; index++)
            {
                if (index > 0)
                {
                    counts.Add(Text(x, 0, CountSeparator.Length, CountSeparator, Palette.Grey));
                    x += CountSeparator.Length;
                }

                counts.Add(Text(x, 0, parts[index].Text.Length, parts[index].Text, Palette.ForTone(parts[index].Tone)));
                x += parts[index].Text.Length;
            }
        }

        /// <summary>The two rows of the key bar: each item a blue key and a white word, both answering a click with the key and a hover with its tip; rebuilt only when the items change.</summary>
        private void ShowBar(KeyBar keys)
        {
            if (keys == shownBar || (shownBar is not null && keys.First.SequenceEqual(shownBar.First) && keys.Second.SequenceEqual(shownBar.Second)))
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
                Label key = Text(item.X, row, item.Key.Length, item.Key, Palette.Blue);
                Label word = Text(item.X + item.Key.Length + 1, row, item.Word.Length, item.Word, Palette.White);
                foreach (Label label in new[] { key, word })
                {
                    label.HotKeySpecifier = new System.Text.Rune(0xFFFF);
                    label.MouseEvent += (_, mouse) => Click(mouse, item);
                    app.ToolTips?.SetToolTip(label, () => ToolTipView(item.Tip));
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
            Pressed(item.Key switch
            {
                "Enter" => Key.Enter,
                "Esc" => Key.Escape,
                _ => Key.Of(item.Key[0]),
            });
        }

        /// <summary>
        /// Names and branches are cut at 28 cells with an ellipsis, so one long value cannot push the other columns off
        /// screen; rows keep the full values. Ages are right-aligned, and every column asks the definition for a cell's own paint.
        /// </summary>
        private void StyleColumns()
        {
            string[] capped = ["NAME", "BRANCH"];
            for (int column = 0; column < columnNames.Length; column++)
            {
                ColumnStyle style = table.Style.GetOrCreateColumnStyle(column);
                style.ColorGetter = CellColour;
                if (capped.Contains(columnNames[column]))
                {
                    style.MaxWidth = LongColumnCap;
                    style.TruncationIndicator = "…";
                }

                if (columnNames[column] == "AGE")
                {
                    style.Alignment = Alignment.End;
                }
            }
        }

        /// <summary>
        /// The cells the elastic column may have: the table's content width minus the other columns' widest visible cells (capped as the
        /// styles cap them), a separator per column and the frame, never below the floor. The elastic column's style is capped to it too.
        /// </summary>
        private int ElasticWidth(IReadOnlyList<TRow> rows, DateTimeOffset now)
        {
            if (definition.ElasticColumn is not int elastic)
            {
                return int.MaxValue;
            }

            int contentWidth = table.Viewport.Width > 0 ? table.Viewport.Width : ScreenWidth - 3;
            int others = 0;
            for (int column = 0; column < columnNames.Length; column++)
            {
                if (column != elastic)
                {
                    others += WidestCell(rows, column, now);
                }
            }

            // The table sizes a column to its content, so the minimum is what makes the elastic column take the spare width.
            int width = Math.Max(contentWidth - others - columnNames.Length - 1, ElasticFloor);
            ColumnStyle style = table.Style.GetOrCreateColumnStyle(elastic);
            style.MaxWidth = width;
            style.MinWidth = width;

            return width;
        }

        private int WidestCell(IReadOnlyList<TRow> rows, int column, DateTimeOffset now)
        {
            int widest = Math.Max(columnNames[column].Length, rows.Count == 0 ? 0 : rows.Max(row => definition.Cell(row, column, now, int.MaxValue).Length));
            ColumnStyle? style = table.Style.GetColumnStyleIfAny(column);

            return style?.MaxWidth is int cap ? Math.Min(widest, cap) : widest;
        }

        /// <summary>A cell's own paint, from the definition: a fill (the TAB swatch) shows on every row, the selected one included; a foreground colour only off the selected row, which keeps its highlight.</summary>
        private Scheme? CellColour(CellColorGetterArgs args)
        {
            if (args.Table is not TableSource<TRow> source)
            {
                return null;
            }

            TRow row = source.RowAt(args.RowIndex);
            CellPaint? paint = definition.CellPaint(row, args.ColIdex);

            return paint switch
            {
                { Fill: Color fill } => Palette.Fill(paint.Foreground, fill),
                not null when !Equals(row, selected) => Palette.Plain(paint.Foreground),
                _ => null,
            };
        }

        /// <summary>
        /// A tooltip drawn in the crumb's colours, so it reads as a tooltip and not as more screen text: each line keeps
        /// its breaks and a cell of margin either side, and the view is exactly as tall as its lines.
        /// </summary>
        private static Label ToolTipView(string text)
        {
            string[] lines = [.. text.Split('\n').Select(line => $" {line} ")];
            Label label = new() { Width = lines.Max(line => line.Length), Height = lines.Length, Text = string.Join('\n', lines) };
            label.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));

            return label;
        }

        private Scheme? RowColour(RowColorGetterArgs args)
        {
            if (args.Table is not TableSource<TRow> source)
            {
                return null;
            }

            TRow row = source.RowAt(args.RowIndex);

            return Equals(row, selected)
                ? Palette.Highlight()
                : Palette.Plain(definition.RowColour(row));
        }

        private static Label Text(Pos x, Pos y, Dim width, string text, Color colour)
        {
            Label label = new() { X = x, Y = y, Width = width, Height = Dim.Auto(), Text = text };
            label.SetScheme(Palette.Plain(colour));

            return label;
        }
    }
}
