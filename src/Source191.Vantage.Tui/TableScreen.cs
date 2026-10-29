using Source191.Vantage.Application;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
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

        using Live live = new(definition, context);
        live.ShowToolTips(app);
        Running running = new(this, app, live, initial);
        live.Window.KeyDown += (_, key) => running.OnKey(key);
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

        public string HelpText => screen.definition.HelpText;

        public TState State { get; private set; } = initial;

        public ViewEffect? Exit { get; private set; }

        public void TriggerLoad()
        {
            Task.Run(async () =>
            {
                ViewInput input;
                try
                {
                    input = await screen.load(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    input = new LoadFailed(exception.Message);
                }

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
            live.Scroll(mapped);
            Apply(new KeyPressed(mapped));
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
                T? result = default;
                Footer? failure = null;
                try
                {
                    result = await action(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    failure = new Footer.Failed(exception.Message);
                }

                app.Invoke(() =>
                {
                    live.ShowBusy(false);
                    State = screen.definition.WithFooter(State, failure ?? onUiThread(result!));
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
                    case ILeavesScreen:
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
        private readonly IScreenDefinition<TState, TRow> definition;
        private readonly ScreenContext context;
        private readonly List<(Label Label, KeyHelp Key)> keyLabels = [];
        private readonly TableView table;
        private readonly Label contextValues;
        private readonly Label title;
        private readonly Label banner;
        private readonly Label footer;
        private readonly SpinnerView busy;
        private readonly WaitingIndicator waiting;
        private TRow? selected;
        private readonly string[] columnNames;
        private int? headerUnderMouse;
        private int columnOffset;

        public Live(IScreenDefinition<TState, TRow> definition, ScreenContext context)
        {
            this.definition = definition;
            this.context = context;
            columnNames = [.. definition.Columns.Select(column => column.Name)];

            // Terminal.Gui writes the top-level's Title to the terminal (OSC 0) on every run, so this is what the tab shows.
            Window = new Window { BorderStyle = LineStyle.None, Title = context.Title };
            Window.SetScheme(Palette.Plain(Palette.White));

            Label contextNames = Text(1, 0, 14, definition.HeaderNames, Palette.Orange);
            contextValues = Text(16, 0, 45, context.HeaderValues(), Palette.White);
            Label logo = Text(Pos.AnchorEnd(12), 0, 11, " VANTAGE\n ◢◤◢◤◢◤\n", Palette.Orange);

            banner = Text(1, 6, Dim.Fill(), string.Empty, Palette.Red);
            title = Text(2, 7, Dim.Fill(), string.Empty, Palette.Cyan);

            table = new TableView
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
            table.Style.RowColorGetter = RowColour;
            CapLongColumns();

            int crumbWidth = definition.Crumb.Length;
            Label crumb = Text(1, Pos.AnchorEnd(2), crumbWidth, definition.Crumb, Palette.Background);
            crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
            busy = Spinners.Create(crumbWidth + 2, Pos.AnchorEnd(2));
            waiting = new WaitingIndicator(definition.WaitingText);
            footer = Text(crumbWidth + 4, Pos.AnchorEnd(2), Dim.Fill(), string.Empty, Palette.Cyan);

            AddKeyHints();
            Window.Add(contextNames, contextValues, logo, banner, title, table, crumb, busy, footer, waiting);
        }

        public Window Window { get; }

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
            table.Table = new TableSource<TRow>(rows, columnNames, (row, column) => definition.Cell(row, column, now));
            table.ColumnOffset = columnOffset;
            contextValues.Text = $"{context.HeaderValues()}\n{definition.ContextLines(state)}";
            string described = Wording.Describe(state.Title);
            title.Text = state.List.FilterEditing ? $"{described}  filter: {state.List.Filter.Text}_" : described;
            banner.Text = state.Banner is null ? string.Empty : $"  {definition.LoadFailurePrefix}: {state.Banner}";
            footer.Text = Wording.Describe(state.Shown);
            if (rows.Count > 0)
            {
                int index = selected is null ? 0 : Math.Max(rows.ToList().IndexOf(selected), 0);
                table.SetSelection(0, index, false, null);
            }

            waiting.Show(!state.Loaded && state.Banner is null);
            Window.SetNeedsDraw();
        }

        /// <summary>Shows each key's tip as a tooltip while the mouse rests on its key or its words, and a column's meaning while it rests on the column's header.</summary>
        public void ShowToolTips(IApplication app)
        {
            foreach ((Label label, KeyHelp key) in keyLabels)
            {
                app.ToolTips?.SetToolTip(label, () => ToolTipView(key.Tip));
            }

            table.MousePositionTracking = true;
            table.MouseEvent += (_, mouse) => ShowColumnToolTip(app, mouse);
            table.MouseLeave += (_, _) => HideColumnToolTip(app);
        }

        public void Dispose() => Window.Dispose();

        private void HideColumnToolTip(IApplication app)
        {
            if (headerUnderMouse is not null)
            {
                app.ToolTips?.Hide();
            }

            headerUnderMouse = null;
        }

        private void ShowColumnToolTip(IApplication app, Terminal.Gui.Input.Mouse mouse)
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

            HideColumnToolTip(app);
            headerUnderMouse = header;
            if (header is int column && column < definition.Columns.Count)
            {
                ColumnHelp help = definition.Columns[column];
                app.ToolTips?.ShowFor(table, new ToolTipProvider(() => ToolTipView($"{help.Name}: {help.Meaning}")));
            }
        }

        /// <summary>Names, repositories, folders and branches are cut at 28 cells with an ellipsis, so one long value cannot push the other columns off screen; rows keep the full values.</summary>
        private void CapLongColumns()
        {
            string[] capped = ["NAME", "REPOSITORY", "FOLDER", "BRANCH"];
            for (int column = 0; column < columnNames.Length; column++)
            {
                if (capped.Contains(columnNames[column]))
                {
                    ColumnStyle style = table.Style.GetOrCreateColumnStyle(column);
                    style.MaxWidth = 28;
                    style.TruncationIndicator = "…";
                }
            }
        }

        /// <summary>One key label and one hint label per row, in the columns the keys name.</summary>
        private void AddKeyHints()
        {
            foreach (IGrouping<int, KeyHelp> column in definition.Keys.Where(key => key.Hint is not null).GroupBy(key => key.Column))
            {
                (int keyX, int keyWidth, int hintX, int hintWidth, int _) = KeyHintColumns.Layout[column.Key];
                int row = 0;
                foreach (KeyHelp key in column)
                {
                    Label keyLabel = Text(keyX, row, keyWidth, key.HeaderKey, Palette.Blue);
                    Label hintLabel = Text(hintX, row, hintWidth, key.Hint!, Palette.White);
                    keyLabels.Add((keyLabel, key));
                    keyLabels.Add((hintLabel, key));
                    Window.Add(keyLabel, hintLabel);
                    row++;
                }
            }
        }

        /// <summary>
        /// A tooltip drawn in the crumb's colours, so it reads as a tooltip and not as more header text: each line keeps
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