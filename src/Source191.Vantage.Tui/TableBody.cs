using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
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

/// <summary>What the console asks of a tab, whatever its rows: its view, its words for the frame, its keys and its refresh.</summary>
internal interface ITabBody
{
    View View { get; }

    TimeSpan RefreshEvery { get; }

    string Title { get; }

    IReadOnlyList<TextPart> Counts { get; }

    /// <summary>The table's title for the frame's line, or the failure banner in red when the last load failed.</summary>
    TextPart FrameTitle { get; }

    KeyBar Bar(bool updateKnown);

    /// <summary>What the console's status line says while this tab is shown: its last message, or its standing hint.</summary>
    StatusMessage Status { get; }

    /// <summary>Whether an action this tab started is still running, which the status line shows with its spinner.</summary>
    bool Running { get; }

    void OnKey(GuiKey key);

    void Press(Key key);

    /// <summary>An input for the tab's reducer that is not a key, such as the status line's close mark or a success's time running out.</summary>
    void Reduce(ViewInput input);

    void TriggerLoad();

    void Redraw();

    /// <summary>
    /// Tells the tab whether it is the one shown. A hidden tab keeps its spinners still: a spinner redraws its own tab alone,
    /// title and frame included, and a hidden tab drawn alone lands on top of the shown one and closes its edge.
    /// </summary>
    void Shown(bool shown);
}

/// <summary>Where a tab hands what is not its own: an effect that leaves the console, a tab switch, and a change the frame must redraw.</summary>
internal interface ITabHost
{
    void Leave(ViewEffect effect);

    void Switch(ISwitchesTab effect);

    void FrameChanged();
}

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
    private readonly IDetailSource<TRow>? detail;
    private readonly DetailRequests? requests;
    private readonly DetailPane? pane;
    private readonly CancellationTokenSource lifetime = new();

    /// <summary>
    /// What a tooltip hangs from: Terminal.Gui draws a tooltip under its target's frame, and under the whole table that is
    /// the detail pane, so the target is this view, as wide as nothing, laid over the header or the row under the pointer.
    /// </summary>
    private readonly View toolTipAnchor = new() { X = 0, Y = 0, Width = 0, Height = HeaderRows, CanFocus = false };
    private object? detailKey;
    private TRow? selected;
    private int? headerUnderMouse;
    private int? rowUnderMouse;
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
        detail = definition.Detail;

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
        if (detail is not null)
        {
            requests = new DetailRequests();
            // The pane closes the tab: nothing is drawn under it inside the frame, since the status line is the console's.
            pane = new DetailPane(detail.Rows);
            pane.Y = Pos.AnchorEnd(pane.Rows);
            table.Height = Dim.Fill(pane.Rows);
            View.Add(pane);
        }

        ShowColumnToolTips();
    }

    public View View { get; }

    public TState State { get; private set; }

    public TimeSpan RefreshEvery => definition.RefreshEvery;

    public string Title => definition.TabTitle(State);

    public IReadOnlyList<TextPart> Counts => definition.Counts(State);

    public TextPart FrameTitle => State.Banner is null
        ? new TextPart(DescribedTitle, TextTone.Cyan)
        : new TextPart($"{definition.LoadFailurePrefix}: {State.Banner}", TextTone.Red);

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
                object? before = detailKey;
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
        Key? mapped = KeyMapping.From(key);
        if (mapped is null)
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
            detailKey = null;
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

        waiting.Show(isShown && !State.Loaded && State.Banner is null);
        AskForDetail(refreshed: false);
        View.SetNeedsDraw();
        host.FrameChanged();
    }


    /// <summary>
    /// Asks for the selected row's detail while the tab is shown (a hidden tab draws nothing of its own, or it lands on top
    /// of the shown one): when the selection moved, or for every refresh; an empty table empties the pane.
    /// </summary>
    private void AskForDetail(bool refreshed)
    {
        if (detail is not { } source || requests is not { } requested || pane is not { } detailPane || !isShown)
        {
            return;
        }

        if (State.List.Selected is not TRow row)
        {
            requested.Cleared();
            detailKey = null;
            detailPane.Show(null);
            return;
        }

        object key = source.KeyOf(row);
        if (!refreshed && Equals(key, detailKey))
        {
            return;
        }

        detailKey = key;
        ReadDetail(source, row, requested, requested.Ask(key), detailPane);
    }

    /// <summary>Reads one request's detail off the UI thread and shows it there, unless a newer answer or another row came first.</summary>
    private void ReadDetail(IDetailSource<TRow> source, TRow row, DetailRequests requested, DetailRequest request, DetailPane detailPane)
    {
        CancellationToken token = lifetime.Token;
        Task.Run(async () =>
        {
            DetailContent? content = await Attempt.RunAsync<DetailContent?>(Diagnostics, FailureSite.Load, CodeName.For(typeof(DetailContent)), () => ReadAsync(source, row, token), _ => null);
            if (content is null || token.IsCancellationRequested)
            {
                return;
            }

            app.Invoke(() => ShowDetail(requested, request, content, detailPane));
        }, token);
    }

    private static async Task<DetailContent?> ReadAsync(IDetailSource<TRow> source, TRow row, CancellationToken token)
    {
        try
        {
            return await source.ReadAsync(row, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
    }

    private void ShowDetail(DetailRequests requested, DetailRequest request, DetailContent content, DetailPane detailPane)
    {
        if (lifetime.IsCancellationRequested || requested.Accept(request, content) is not DetailAnswer.Show shown)
        {
            return;
        }

        detailPane.Show(shown.Content);
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

    /// <summary>Left and Right move the table one column sideways, within its columns; presentation only, so the reducer never sees a change.</summary>
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
        if (headerUnderMouse is not null || rowUnderMouse is not null)
        {
            app.ToolTips?.Hide();
        }

        headerUnderMouse = null;
        rowUnderMouse = null;
    }

    /// <summary>A column's meaning while the pointer rests on its header; a row's tip, when the view gives one, while it rests on the row.</summary>
    private void ShowColumnToolTip(Terminal.Gui.Input.Mouse mouse)
    {
        int? header = null;
        System.Drawing.Point? cell = null;
        if (mouse.Position is System.Drawing.Point position)
        {
            cell = table.ScreenToCell(position, out header);
        }

        int? row = header is null ? cell?.Y : null;
        if (header == headerUnderMouse && row == rowUnderMouse)
        {
            return;
        }

        HideColumnToolTip();
        headerUnderMouse = header;
        rowUnderMouse = row;
        int x = table.Frame.X + (mouse.Position?.X ?? 0);
        if (header is int column && column < definition.Columns.Count)
        {
            ColumnHelp help = definition.Columns[column];
            ShowToolTip(x, table.Frame.Y, HeaderRows, $"{help.Name}: {help.Meaning}");
            return;
        }

        if (row is int index && table.Table is TableSource<TRow> source && index < source.Rows && definition.RowTip(source.RowAt(index)) is string tip)
        {
            ShowToolTip(x, table.Frame.Y + (mouse.Position?.Y ?? 0), 1, tip);
        }
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
        CellPaint? paint = definition.CellPaint(row, args.ColIdex);

        return paint switch
        {
            { Always: true } when Equals(row, selected) => Palette.SelectedRow(paint.Foreground),
            not null when !Equals(row, selected) => Palette.Plain(paint.Foreground),
            _ => null,
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
}

/// <summary>Vantage's tooltips: black on amber, so they read as tooltips and not as more screen text.</summary>
internal static class ToolTips
{
    /// <summary>Each line keeps its breaks and a cell of margin either side, and the view is exactly as tall as its lines.</summary>
    public static Label View(string text)
    {
        string[] lines = [.. text.Split('\n').Select(line => $" {line} ")];
        Label label = new() { Width = lines.Max(line => line.Length), Height = lines.Length, Text = string.Join('\n', lines) };
        label.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));

        return label;
    }
}