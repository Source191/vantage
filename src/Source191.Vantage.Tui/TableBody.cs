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

    void OnKey(GuiKey key);

    void Press(Key key);

    void TriggerLoad();

    void Redraw();
}

/// <summary>Where a tab hands what is not its own: an effect that leaves the console, a tab switch, and a change the frame must redraw.</summary>
internal interface ITabHost
{
    void Leave(ViewEffect effect);

    void Switch(ISwitchesTab effect);

    void FrameChanged();
}

/// <summary>
/// One tab of the console: a view's rows in a table with its crumb, footer and waiting indicator (its title goes on the frame), the reducer it
/// feeds keys to, and the effects it routes (a dialog to <see cref="DialogEffects"/>, a background effect to its handler
/// through the <see cref="EffectDispatcher"/> with a busy spinner, a tab switch or an exit to the console). A definition
/// supplies everything specific to the view.
/// </summary>
internal sealed class TableBody<TState, TRow> : ITabBody, IEffectContext, IDisposable
    where TState : IListViewState<TRow>
{
    private const int LongColumnCap = 28;
    private const int ElasticFloor = 20;

    private readonly IScreenDefinition<TState, TRow> definition;
    private readonly DialogEffects dialogs;
    private readonly EffectDispatcher dispatcher;
    private readonly Func<CancellationToken, Task<ViewInput>> load;
    private readonly TimeProvider clock;
    private readonly ScreenContext context;
    private readonly IApplication app;
    private readonly ITabHost host;
    private readonly TableView table;
    private readonly Label footer;
    private readonly SpinnerView busy;
    private readonly WaitingIndicator waiting;
    private readonly string[] columnNames;
    private TRow? selected;
    private int? headerUnderMouse;
    private int columnOffset;

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

        // The tab's title is the view's Title; it can take focus because Tabs shows whichever tab has it.
        View = new View { Title = definition.TabTitle(initial), Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = true };
        View.SetScheme(Palette.Plain(Palette.White));

        table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
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

        int crumbWidth = definition.Crumb.Length;
        Label crumb = Text(0, Pos.AnchorEnd(1), crumbWidth, definition.Crumb, Palette.Background);
        crumb.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));
        busy = Spinners.Create(crumbWidth + 1, Pos.AnchorEnd(1));
        waiting = new WaitingIndicator(definition.WaitingText);
        footer = Text(crumbWidth + 3, Pos.AnchorEnd(1), Dim.Fill(), string.Empty, Palette.Cyan);

        View.Add(table, crumb, busy, footer, waiting);
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

    /// <summary>Disposes the tab's view and everything in it; the console removes it from the window first, so nothing is disposed twice.</summary>
    public void Dispose() => View.Dispose();

    public void TriggerLoad()
    {
        Task.Run(async () =>
        {
            ViewInput input = await Attempt.RunAsync(Diagnostics, FailureSite.Load, CodeName.For(typeof(TState)), () => load(CancellationToken.None), exception => new LoadFailed(exception.Message));

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
        Scroll(key);
        Apply(new KeyPressed(key));
    }

    public void Reduce(ViewInput input) => Apply(input);

    public void ShowFooter(Footer footer) => State = definition.WithFooter(State, footer);

    public void Dispatch(ViewEffect effect) => Dispatch(effect, outcome => new Footer.Reported(outcome));

    public void Dispatch(ViewEffect effect, Func<Outcome, Footer> onUiThread) => RunInBackground(new Footer.Working(effect), token => dispatcher.DispatchAsync(effect, token), onUiThread);

    public void RunInBackground<T>(Footer working, Func<CancellationToken, Task<T>> action, Func<T, Footer> onUiThread)
    {
        State = definition.WithFooter(State, working);
        Redraw();
        busy.Show(true);
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
                busy.Show(false);
                State = definition.WithFooter(State, settle());
                Redraw();
                TriggerLoad();
            });
        });
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
        footer.Text = Wording.Describe(State.Shown);
        if (rows.Count > 0)
        {
            int index = selected is null ? 0 : Math.Max(rows.ToList().IndexOf(selected), 0);
            table.SetSelection(0, index, false, null);
        }

        waiting.Show(!State.Loaded && State.Banner is null);
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
    private void Scroll(Key key)
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

    /// <summary>Shows a column's meaning as a tooltip while the mouse rests on the column's header.</summary>
    private void ShowColumnToolTips()
    {
        table.MousePositionTracking = true;
        table.MouseEvent += (_, mouse) => ShowColumnToolTip(mouse);
        table.MouseLeave += (_, _) => HideColumnToolTip();
    }

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
            app.ToolTips?.ShowFor(table, new ToolTipProvider(() => ToolTips.View($"{help.Name}: {help.Meaning}")));
        }
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

    private static Label Text(Pos x, Pos y, Dim width, string text, Color colour)
    {
        Label label = new() { X = x, Y = y, Width = width, Height = Dim.Auto(), Text = text };
        label.SetScheme(Palette.Plain(colour));

        return label;
    }
}

/// <summary>Vantage's tooltips: drawn in the crumb's colours, so they read as tooltips and not as more screen text.</summary>
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