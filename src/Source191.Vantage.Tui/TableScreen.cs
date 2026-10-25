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
    string context)
    where TState : IListViewState<TRow>
{
    private readonly IScreenDefinition<TState, TRow> definition = definition;
    private readonly DialogEffects dialogs = dialogs;
    private readonly EffectDispatcher dispatcher = dispatcher;
    private readonly Func<CancellationToken, Task<ViewInput>> load = load;
    private readonly TimeProvider clock = clock;
    private readonly string context = context;

    public ScreenRun<TState> Run(TState initial)
    {
        using IApplication app = Terminal.Gui.App.Application.Create().Init();
        // Windows Terminal renders 24-bit colour; without this the driver falls back to 16 colours and the palette shifts.
        if (app.Driver is not null)
        {
            app.Driver.Force16Colors = false;
        }

        using Live live = new(definition, context);
        Running running = new(this, app, live, initial);
        live.Window.KeyDown += (_, key) => running.OnKey(key);
        running.Redraw();

        object? timer = app.AddTimeout(definition.RefreshEvery, () =>
        {
            running.TriggerLoad();
            return true;
        });
        running.TriggerLoad();

        app.Run(live.Window);
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
        private readonly string context;
        private readonly TableView table;
        private readonly Label contextValues;
        private readonly Label title;
        private readonly Label banner;
        private readonly Label footer;
        private readonly SpinnerView busy;
        private readonly WaitingIndicator waiting;
        private TRow? selected;

        public Live(IScreenDefinition<TState, TRow> definition, string context)
        {
            this.definition = definition;
            this.context = context;

            // Terminal.Gui writes the top-level's Title to the terminal (OSC 0) on every run, so this is what the tab shows.
            Window = new Window { BorderStyle = LineStyle.None, Title = "Vantage" };
            Window.SetScheme(Palette.Plain(Palette.White));

            Label contextNames = Text(1, 0, 11, definition.HeaderNames, Palette.Orange);
            contextValues = Text(13, 0, 48, context, Palette.White);
            Label keys = Text(62, 0, 10, definition.Keys, Palette.Blue);
            Label keyNames = Text(72, 0, 16, definition.KeyNames, Palette.White);
            Label keys2 = Text(90, 0, 8, definition.Keys2, Palette.Blue);
            Label keyNames2 = Text(98, 0, 20, definition.KeyNames2, Palette.White);
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

            int crumbWidth = definition.Crumb.Length;
            Label crumb = Text(1, Pos.AnchorEnd(2), crumbWidth, definition.Crumb, Palette.Background);
            crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
            busy = Spinners.Create(crumbWidth + 2, Pos.AnchorEnd(2));
            waiting = new WaitingIndicator(definition.WaitingText);
            footer = Text(crumbWidth + 4, Pos.AnchorEnd(2), Dim.Fill(), string.Empty, Palette.Cyan);

            Window.Add(contextNames, contextValues, keys, keyNames, keys2, keyNames2, logo, banner, title, table, crumb, busy, footer, waiting);
        }

        public Window Window { get; }

        /// <summary>The footer spinner: on while an action runs off the UI thread, off once its outcome is shown.</summary>
        public void ShowBusy(bool working) => busy.Show(working);

        public void Redraw(TState state, DateTimeOffset now)
        {
            IReadOnlyList<TRow> rows = state.List.Visible;
            selected = state.List.Selected;
            table.Table = new TableSource<TRow>(rows, definition.ColumnNames, (row, column) => definition.Cell(row, column, now));
            contextValues.Text = $"{context}\n{definition.ContextLines(state)}";
            title.Text = state.List.FilterEditing ? $"{Wording.Describe(state.Title)}  filter: {state.List.Filter.Text}_" : Wording.Describe(state.Title);
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

        public void Dispose() => Window.Dispose();

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