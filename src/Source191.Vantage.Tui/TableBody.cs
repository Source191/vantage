using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Alignment = Terminal.Gui.ViewBase.Alignment;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// One tab of the console: a view's rows in a table with its waiting indicator (its title goes on the frame), the detail pane under
/// the table when the view has one (read for the selected row off the UI thread, only while the tab is shown), the reducer it
/// feeds keys to, and the effects it routes (a dialog to <see cref="DialogEffects"/>, a background effect to its handler
/// through the <see cref="EffectDispatcher"/>, a tab switch or an exit to the console). Its message and whether an action
/// is running go to the console's status line, outside the tab. A definition supplies everything specific to the view.
/// </summary>
internal sealed class TableBody<TState, TRow> : ITabBody, IEffectContext, IDisposable
    where TState : IListViewState<TRow>
{
    private const int LongColumnCap = 28;
    private const int ElasticFloor = 20;

    /// <summary>The table's header: the overline, the names and the line under them.</summary>
    private const int HeaderRows = 3;

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
    private readonly string[] columnNames;
    private readonly DetailPart detailPart;
    private readonly CancellationTokenSource lifetime = new();

    /// <summary>What <see cref="detailKey"/> holds when the next redraw must ask for the selected row's detail again; no row's key equals it.</summary>
    private readonly object askAgain = new();

    /// <summary>
    /// What a tooltip hangs from: Terminal.Gui draws a tooltip under its target's frame, and under the whole table that is
    /// the detail pane, so the target is this view, as wide as nothing, laid over the header or the row under the pointer.
    /// </summary>
    private readonly View toolTipAnchor = new() { X = 0, Y = 0, Width = 0, Height = HeaderRows, CanFocus = false };

    /// <summary>The key of the row whose detail was last asked for, or <see cref="askAgain"/>.</summary>
    private object detailKey;
    private TRow? selected;
    private PointerOver pointer = new PointerOver.Nothing();
    private int columnOffset;
    private bool isShown;
    private bool running;

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
        columnNames = [.. definition.Columns.Select(column => column.Name)];
        detailKey = askAgain;

        // The tab's title is the view's Title; it can take focus because Tabs shows whichever tab has it.
        View = new View { Title = definition.TabTitle(initial), Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = true };
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
        table.Style.RowColorGetter = RowColour;
        StyleColumns();

        // In the middle of the table, whatever the pane under it takes, so the spinner never lands in the pane.
        waiting = new WaitingIndicator(definition.WaitingText) { Y = Pos.Func(_ => table.Frame.Y + (table.Frame.Height / 2), table) };

        View.Add(table, waiting, toolTipAnchor);
        detailPart = definition.Detail switch
        {
            TableDetail<TRow>.Pane pane => AddPane(pane.Source),
            TableDetail<TRow>.None => new DetailPart.NoPane(),
            _ => throw new UnreachableException(),
        };

        ShowColumnToolTips();
    }

    public View View { get; }

    public TState State { get; private set; }

    public TimeSpan RefreshEvery => definition.RefreshEvery;

    public string Title => definition.TabTitle(State);

    public IReadOnlyList<TextPart> Counts => definition.Counts(State);

    public TextPart FrameTitle => State.Banner is LoadBanner.Failed failed
        ? new TextPart($"{definition.LoadFailurePrefix}: {failed.Message}", TextTone.Red)
        : new TextPart(DescribedTitle, TextTone.Cyan);

    /// <summary>The table's title, with a filter being typed before it, so the typed text stays in view when the title is cut.</summary>
    private string DescribedTitle => State.List.FilterEditing ? $"filter: {State.List.Filter.Text}_  {Wording.Describe(State.Title)}" : Wording.Describe(State.Title);

    public IApplication App => app;

    public DiagnosticLog Diagnostics => context.Diagnostics;

    public string HelpText => definition.HelpText;

    public KeyBar Bar(bool updateKnown) => definition.Bar(State, updateKnown);

    public StatusMessage Status => State.Shown;

    public bool Running => running;

    /// <summary>Drops the detail still being read, then disposes the tab's view and everything in it; the console removes it from the window first, so nothing is disposed twice.</summary>
    public void Dispose()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        View.Dispose();
    }

    public void TriggerLoad()
    {
        Task.Run(async () =>
        {
            ViewInput input = await Attempt.RunAsync(Diagnostics, FailureSite.Load, CodeName.For(typeof(TState)), () => load(CancellationToken.None), exception => new LoadFailed(exception.Message));

            app.Invoke(() =>
            {
                object before = detailKey;
                Apply(input);
                if (Equals(detailKey, before))
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
        if (!State.List.FilterEditing && ScrollStep(mapped) is int step)
        {
            Scroll(step);
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
        running = true;
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
                running = false;
                State = definition.WithMessage(State, settle());
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
            detailKey = askAgain;
        }

        Redraw();
    }

    public void Redraw()
    {
        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TRow> rows = State.List.Visible;
        selected = State.List.Selected;
        int elasticWidth = ElasticWidth(rows, now);
        table.Table = new TableSource<TRow>(rows, columnNames, (row, column) => definition.Cell(row, column, now, column == definition.ElasticColumn ? elasticWidth : int.MaxValue));
        table.ColumnOffset = columnOffset;
        View.Title = definition.TabTitle(State);
        if (rows.Count > 0)
        {
            int index = selected is null ? 0 : Math.Max(rows.ToList().IndexOf(selected), 0);
            table.SetSelection(0, index, false, null);
        }

        waiting.Show(isShown && !State.Loaded && State.Banner is LoadBanner.None);
        AskForDetail(refreshed: false);
        View.SetNeedsDraw();
        host.FrameChanged();
    }


    /// <summary>Builds the pane under the table and its requests; the pane closes the tab, since the status line is the console's.</summary>
    private DetailPart.Pane AddPane(IDetailSource<TRow> source)
    {
        DetailPane pane = new(source.Rows);
        pane.Y = Pos.AnchorEnd(pane.Rows);
        table.Height = Dim.Fill(pane.Rows);
        View.Add(pane);

        return new DetailPart.Pane(source, new DetailRequests(), pane);
    }

    /// <summary>
    /// Asks for the selected row's detail while the tab is shown (a hidden tab draws nothing of its own, or it lands on top
    /// of the shown one): when the selection moved, or for every refresh; an empty table empties the pane.
    /// </summary>
    private void AskForDetail(bool refreshed)
    {
        if (detailPart is not DetailPart.Pane part || !isShown)
        {
            return;
        }

        if (State.List.Selected is not TRow row)
        {
            part.Requests.Cleared();
            detailKey = askAgain;
            part.View.Show(DetailContent.Empty);
            return;
        }

        object key = part.Source.KeyOf(row);
        if (!refreshed && Equals(key, detailKey))
        {
            return;
        }

        detailKey = key;
        ReadDetail(part, row, part.Requests.Ask(key));
    }

    /// <summary>Reads one request's detail off the UI thread and shows it there, unless a newer answer or another row came first.</summary>
    private void ReadDetail(DetailPart.Pane part, TRow row, DetailRequest request)
    {
        CancellationToken token = lifetime.Token;
        Task.Run(async () =>
        {
            DetailAnswer read = await Attempt.RunAsync<DetailAnswer>(Diagnostics, FailureSite.Load, CodeName.For(typeof(DetailContent)), () => ReadAsync(part.Source, row, token), _ => new DetailAnswer.Dropped());
            if (read is not DetailAnswer.Show { Content: DetailContent content } || token.IsCancellationRequested)
            {
                return;
            }

            app.Invoke(() => ShowDetail(part, request, content));
        }, token);
    }

    /// <summary>The row's detail as read, or dropped when the tab was disposed while it was being read.</summary>
    private static async Task<DetailAnswer> ReadAsync(IDetailSource<TRow> source, TRow row, CancellationToken token)
    {
        try
        {
            return new DetailAnswer.Show(await source.ReadAsync(row, token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return new DetailAnswer.Dropped();
        }
    }

    private void ShowDetail(DetailPart.Pane part, DetailRequest request, DetailContent content)
    {
        if (lifetime.IsCancellationRequested || part.Requests.Accept(request, content) is not DetailAnswer.Show shown)
        {
            return;
        }

        part.View.Show(shown.Content);
        View.SetNeedsDraw();
        host.FrameChanged();
    }

    private void Apply(ViewInput input)
    {
        definition.OnInput(State, input);
        (TState state, IReadOnlyList<ViewEffect> effects) = definition.Reduce(State, input);
        State = state;
        foreach (ViewEffect effect in effects)
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

    /// <summary>&lt; and &gt; scroll a table too wide for the terminal (Shift+arrows did not reach Vantage in Windows Terminal); Left and Right are the tab keys.</summary>
    private static int? ScrollStep(Key key) => key switch
    {
        { Name: KeyName.Character, Character: '<' } => -1,
        { Name: KeyName.Character, Character: '>' } => 1,
        _ => null,
    };

    private void Scroll(int step)
    {
        // Kept here as well as on the table, because every redraw replaces the table's source, which resets its offset.
        columnOffset = Math.Clamp(columnOffset + step, 0, columnNames.Length - 1);
        table.ColumnOffset = columnOffset;
        table.SetNeedsDraw();
    }

    /// <summary>Shows a column's meaning as a tooltip while the mouse rests on the column's header.</summary>
    private void ShowColumnToolTips()
    {
        table.MousePositionTracking = true;
        table.MouseEvent += (_, mouse) => ShowColumnToolTip(mouse);
        table.MouseLeave += (_, _) => HideColumnToolTip();
    }

    private void HideColumnToolTip()
    {
        if (pointer is not PointerOver.Nothing)
        {
            app.ToolTips?.Hide();
        }

        pointer = new PointerOver.Nothing();
    }

    /// <summary>A column's meaning while the pointer rests on its header; a row's tip, when the view gives one, while it rests on the row.</summary>
    private void ShowColumnToolTip(Terminal.Gui.Input.Mouse mouse)
    {
        PointerOver over = mouse.Position is System.Drawing.Point position ? PointerAt(position) : new PointerOver.Nothing();
        if (over == pointer)
        {
            return;
        }

        HideColumnToolTip();
        pointer = over;
        int x = table.Frame.X + (mouse.Position?.X ?? 0);
        if (over is PointerOver.Header header && header.Column < definition.Columns.Count)
        {
            ColumnHelp help = definition.Columns[header.Column];
            ShowToolTip(x, table.Frame.Y, HeaderRows, $"{help.Name}: {help.Meaning}");
            return;
        }

        if (over is PointerOver.Row row && table.Table is TableSource<TRow> source && row.Index < source.Rows && definition.RowTip(source.RowAt(row.Index)) is { Length: > 0 } tip)
        {
            ShowToolTip(x, table.Frame.Y + (mouse.Position?.Y ?? 0), 1, tip);
        }
    }

    /// <summary>What the pointer is over, from Terminal.Gui's answer: a header's column, else the row of the cell under it, else nothing.</summary>
    private PointerOver PointerAt(System.Drawing.Point position)
    {
        System.Drawing.Point? cell = table.ScreenToCell(position, out int? header);

        return (header, cell) switch
        {
            (int column, _) => new PointerOver.Header(column),
            (null, System.Drawing.Point { Y: int index }) => new PointerOver.Row(index),
            _ => new PointerOver.Nothing(),
        };
    }

    /// <summary>Lays the anchor over the header or the row at the pointer and shows the tooltip on the row under it.</summary>
    private void ShowToolTip(int x, int y, int height, string text)
    {
        toolTipAnchor.X = x;
        toolTipAnchor.Y = y;
        toolTipAnchor.Height = height;
        View.Layout();
        app.ToolTips?.ShowFor(toolTipAnchor, new ToolTipProvider(() => ToolTips.View(text)));
    }

    /// <summary>
    /// Names are cut at 28 cells with an ellipsis, so one long value cannot push the other columns off
    /// screen; rows keep the full values. Ages are right-aligned, and every column asks the definition for a cell's own paint.
    /// </summary>
    private void StyleColumns()
    {
        for (int column = 0; column < columnNames.Length; column++)
        {
            ColumnStyle style = table.Style.GetOrCreateColumnStyle(column);
            style.ColorGetter = CellColour;
            if (columnNames[column] == "NAME")
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
    /// styles cap them) and a separator per column, never below the floor. The elastic column's style is held to it, both ways.
    /// </summary>
    private int ElasticWidth(IReadOnlyList<TRow> rows, DateTimeOffset now)
    {
        if (definition.ElasticColumn is not int elastic)
        {
            return int.MaxValue;
        }

        int contentWidth = table.Viewport.Width > 0 ? table.Viewport.Width : app.Screen.Width - 2;
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
        bool onSelected = Equals(row, selected);

        // Null is Terminal.Gui's "the row's own scheme applies".
        return definition.CellPaint(row, args.ColIdex) switch
        {
            CellPaint.Fill fill when onSelected => Palette.SelectedRow(fill.Foreground),
            CellPaint.Fill fill => Palette.Plain(fill.Foreground),
            CellPaint.Text when onSelected => null,
            CellPaint.Text text => Palette.Plain(text.Foreground),
            CellPaint.None => null,
            _ => throw new UnreachableException(),
        };
    }

    private Scheme? RowColour(RowColorGetterArgs args)
    {
        if (args.Table is not TableSource<TRow> source)
        {
            return null;
        }

        TRow row = source.RowAt(args.RowIndex);

        return Equals(row, selected)
            ? Palette.SelectedRow()
            : Palette.Plain(definition.RowColour(row));
    }

    /// <summary>What the pointer rests on, so a tooltip is shown once per header or row it enters.</summary>
    private abstract record PointerOver
    {
        public sealed record Nothing : PointerOver;

        public sealed record Header(int Column) : PointerOver;

        public sealed record Row(int Index) : PointerOver;
    }

    /// <summary>The tab's detail pane as the tab holds it: none, or the pane with its source and the order of its requests.</summary>
    private abstract record DetailPart
    {
        /// <summary>No pane: nothing is added under the table, which fills the tab.</summary>
        public sealed record NoPane : DetailPart;

        public sealed record Pane(IDetailSource<TRow> Source, DetailRequests Requests, DetailPane View) : DetailPart;
    }
}