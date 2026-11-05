using Source191.Torni.Application;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Torni.Tui;

/// <summary>
/// One tab of the console: a view's rows in a table with its waiting indicator (its title goes on the frame), the reducer it
/// feeds keys to, and the effects it routes (a dialog to <see cref="DialogEffects"/>, a background effect to its handler
/// through the <see cref="EffectDispatcher"/>, a tab switch or an exit to the console). It keeps the state and the loads. The
/// column styles and widths are <see cref="ColumnSizing{TRow}"/>'s, the header and row tooltips <see cref="TableToolTips{TRow}"/>'s
/// and the detail pane under the table <see cref="DetailReader{TRow}"/>'s. Its message and whether an action
/// is running go to the console's status line, outside the tab. A definition supplies everything specific to the view.
/// </summary>
internal sealed class TableBody<TState, TRow> : ITabBody, IEffectContext, IDisposable
    where TState : IListViewState<TRow>
{
    private readonly IScreenDefinition<TState, TRow> definition;
    private readonly DialogEffects dialogs;
    private readonly EffectDispatcher dispatcher;
    private readonly Func<CancellationToken, Task<ViewInput>> load;
    private readonly TimeProvider clock;
    private readonly ScreenContext context;
    private readonly IApplication app;
    private readonly ITabHost host;
    private readonly TableView table;
    private readonly WaitingIndicator waiting;
    private readonly ColumnSizing<TRow> sizing;
    private readonly DetailReader<TRow> detail;
    private readonly LoadSequence sequence = new();
    private bool isShown;
    private int inFlight;

    public TableBody(IScreenDefinition<TState, TRow> definition, DialogEffects dialogs, EffectDispatcher dispatcher, Func<CancellationToken, Task<ViewInput>> load, TimeProvider clock, ScreenContext context, IApplication app, ITabHost host, TState initial)
    {
        this.definition = definition;
        this.dialogs = dialogs;
        this.dispatcher = dispatcher;
        this.load = load;
        this.clock = clock;
        this.context = context;
        this.app = app;
        this.host = host;
        State = initial;

        // The tab's title is the view's Title; it can take focus because Tabs shows whichever tab has it.
        View = new View { Title = Wording.TabTitleFor(definition.Tab), Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = true };
        View.SetScheme(Palette.Plain(Palette.White));

        table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            FullRowSelect = true,
            MultiSelect = false,
        };
        table.Style.ShowVerticalCellLines = false;
        // The header is a closed box in the tab frames' grey, its names white: lines take the table's scheme, the names the header's, and every row its own colour.
        table.Style.ShowHorizontalHeaderOverline = true;
        table.Style.HeaderScheme = Palette.Plain(Palette.White);
        table.Style.ExpandLastColumn = true;
        table.SetScheme(Palette.Plain(Palette.Grey));
        sizing = new ColumnSizing<TRow>(table, definition.Columns, definition.RowColour, app);
        TableToolTips<TRow> toolTips = new(table, app, [.. definition.Columns.Select(column => column.Help)], definition.RowTip);
        detail = DetailReader<TRow>.For(definition.Detail, table, app, context.Diagnostics, host);

        // In the middle of the table, whatever the pane under it takes, so the spinner never lands in the pane.
        waiting = new WaitingIndicator(definition.WaitingText) { Y = Pos.Func(_ => table.Frame.Y + (table.Frame.Height / 2), table) };

        View.Add(table, waiting, toolTips.Anchor);
        foreach (View part in detail.Views)
        {
            View.Add(part);
        }
    }

    public View View { get; }

    public TState State { get; private set; }

    public TabRefresh Refresh => definition.Refresh;

    public IReadOnlyList<TextPart> Counts => definition.Counts(State);

    public TextPart FrameTitle => State.Banner is LoadBanner.Failed failed
        ? new TextPart($"{definition.LoadFailurePrefix}: {failed.Message}", TextTone.Red)
        : new TextPart(DescribedTitle, TextTone.Cyan);

    /// <summary>The table's title, with a filter being typed before it, so the typed text stays in view when the title is cut.</summary>
    private string DescribedTitle => State.List.FilterEditing ? Wording.FilterBeingTyped(State.List.Filter.Text, State.Title) : Wording.Describe(State.Title);

    public IApplication App => app;

    public DiagnosticLog Diagnostics => context.Diagnostics;

    public string HelpText => definition.HelpText;

    public KeyBar Bar(bool updateKnown) => definition.Bar(State, updateKnown);

    public StatusMessage Status => State.Message;

    public bool Running => inFlight > 0;

    /// <summary>Drops the detail still being read, then disposes the tab's view and everything in it; the console removes it from the window first, so nothing is disposed twice.</summary>
    public void Dispose()
    {
        detail.Release();
        View.Dispose();
    }

    public void TriggerLoad()
    {
        int ticket = sequence.Begin();
        Task.Run(async () =>
        {
            ViewInput input = await Attempt.RunAsync(Diagnostics, FailureSite.Load, CodeName.For(typeof(TState)), () => load(CancellationToken.None), exception => new LoadFailed(exception.Message));

            app.Invoke(() =>
            {
                if (!sequence.Accept(ticket))
                {
                    return;
                }

                object before = detail.Current;
                Apply(input);
                if (Equals(detail.Current, before))
                {
                    AskForDetail(refreshed: true);
                }
            });
        }, CancellationToken.None);
    }

    public void OnKey(GuiKey key)
    {
        if (!KeyMapping.TryFrom(key, out Key? mapped))
        {
            return;
        }

        key.Handled = true;
        if (!State.List.FilterEditing && ScrollKeys.TryFor(mapped, out int step))
        {
            sizing.Scroll(step);
            return;
        }

        Press(mapped);
    }

    /// <summary>A key from the keyboard or a click on the key bar, for the reducer; Left and Right move between tabs there.</summary>
    public void Press(Key key) => Apply(new KeyPressed(key));

    public void Reduce(ViewInput input) => Apply(input);

    public void ShowMessage(StatusMessage message) => State = definition.WithMessage(State, message);

    public void Dispatch(ViewEffect effect) => Dispatch(effect, outcome => new StatusMessage.Reported(outcome));

    public void Dispatch(ViewEffect effect, Func<Outcome, StatusMessage> onUiThread) => RunInBackground(new StatusMessage.Working(effect), token => dispatcher.DispatchAsync(effect, token), onUiThread);

    public void RunInBackground<T>(StatusMessage working, Func<CancellationToken, Task<T>> action, Func<T, StatusMessage> onUiThread)
    {
        State = definition.WithMessage(State, working);
        inFlight++;
        Redraw();
        Task.Run(async () =>
        {
            // The continuation is built here but run on the UI thread, where onUiThread must run.
            Func<StatusMessage> settle = await Attempt.RunAsync<Func<StatusMessage>>(
                Diagnostics,
                FailureSite.Effect,
                Attempt.What(working),
                async () =>
                {
                    T result = await action(CancellationToken.None);
                    return () => onUiThread(result);
                },
                exception => () => new StatusMessage.Failed(exception.Message));

            app.Invoke(() =>
            {
                inFlight--;
                StatusMessage settled = settle();
                State = definition.WithMessage(State, settled);
                Redraw();
                TriggerLoad();
            });
        }, CancellationToken.None);
    }

    /// <summary>A tab shown again reads its selected row's detail afresh.</summary>
    public void Shown(bool shown)
    {
        isShown = shown;
        if (shown)
        {
            detail.AskAgain();
        }

        Redraw();
    }

    public void Redraw()
    {
        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TRow> rows = State.List.Visible;
        TRow? selected = State.List.Selected;
        sizing.Selected(selected);
        table.Table = sizing.Source(rows, now);
        sizing.RestoreOffset();
        if (rows.Count > 0)
        {
            int index = selected is null ? 0 : Math.Max(rows.ToList().IndexOf(selected), 0);
            table.SetSelection(0, index, false, null);
        }

        waiting.Show(isShown && !State.List.Loaded && State.Banner is LoadBanner.None);
        AskForDetail(refreshed: false);
        View.SetNeedsDraw();
        host.FrameChanged();
    }

    private void AskForDetail(bool refreshed) => detail.AskFor(State.List.Selected, isShown, refreshed);

    private void Apply(ViewInput input)
    {
        definition.OnInput(State, input);
        ViewUpdate<TState> update = definition.Reduce(State, input);
        State = update.State;
        foreach (ViewEffect effect in update.Effects)
        {
            switch (effect)
            {
                case ILeavesScreen:
                    host.Leave(effect);
                    return;
                case ISwitchesTab switching:
                    Redraw();
                    host.Switch(switching);
                    return;
                case IConsultsSessions consulting:
                    Follow(host.Consult(consulting));
                    break;
                case IDialogEffect:
                    dialogs.Show(this, effect);
                    break;
                default:
                    Dispatch(effect);
                    break;
            }
        }

        Redraw();
    }

    /// <summary>The Sessions tab's answer, run from this tab: the effect goes to its handler, so its outcome shows on this tab's status line, or the reason is shown here.</summary>
    private void Follow(RowDecision decision)
    {
        switch (decision)
        {
            case RowDecision.Act act:
                Dispatch(act.Effect);
                break;
            case RowDecision.Explain explain:
                ShowMessage(explain.Message);
                break;
            default:
                throw new UnreachableException();
        }
    }
}