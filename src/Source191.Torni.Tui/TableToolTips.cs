using Source191.Torni.Application;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>
/// A table's header and row tooltips: a column's meaning while the pointer rests on its header, and a row's tip, when the view gives
/// one, while it rests on the row. It owns the zero-width anchor a tooltip hangs from and what the pointer was last over, and
/// listens to the table's mouse. Terminal.Gui draws a tooltip under its target's frame, and under the whole table that is the
/// detail pane, so the target is the <see cref="Anchor"/>, laid over the header or the row under the pointer.
/// </summary>
internal sealed class TableToolTips<TRow>
{
    /// <summary>The table's header: the overline, the names and the line under them.</summary>
    private const int HeaderRows = 3;

    private readonly TableView table;
    private readonly IApplication app;
    private readonly IReadOnlyList<ColumnHelp> columns;
    private readonly Func<TRow, string> rowTip;
    private PointerOver pointer = new PointerOver.Nothing();

    public TableToolTips(TableView table, IApplication app, IReadOnlyList<ColumnHelp> columns, Func<TRow, string> rowTip)
    {
        this.table = table;
        this.app = app;
        this.columns = columns;
        this.rowTip = rowTip;
        table.MousePositionTracking = true;
        table.MouseEvent += (_, mouse) => Show(mouse);
        table.MouseLeave += (_, _) => Hide();
    }

    /// <summary>What a tooltip hangs from, for the tab to add to its view.</summary>
    public View Anchor { get; } = new() { X = 0, Y = 0, Width = 0, Height = HeaderRows, CanFocus = false };

    private void Hide()
    {
        if (pointer is not PointerOver.Nothing)
        {
            app.ToolTips?.Hide();
        }

        pointer = new PointerOver.Nothing();
    }

    /// <summary>A column's meaning while the pointer rests on its header; a row's tip, when the view gives one, while it rests on the row.</summary>
    private void Show(Terminal.Gui.Input.Mouse mouse)
    {
        PointerOver over = mouse.Position is System.Drawing.Point position ? PointerAt(position) : new PointerOver.Nothing();
        if (over == pointer)
        {
            return;
        }

        Hide();
        pointer = over;
        int x = table.Frame.X + (mouse.Position?.X ?? 0);
        if (over is PointerOver.Header header && header.Column < columns.Count)
        {
            ColumnHelp help = columns[header.Column];
            ShowToolTip(x, table.Frame.Y, HeaderRows, $"{help.Name}: {help.Meaning}");
            return;
        }

        if (over is PointerOver.Row row && table.Table is TableSource<TRow> source && row.Index < source.Rows && rowTip(source.RowAt(row.Index)) is { Length: > 0 } tip)
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
        Anchor.X = x;
        Anchor.Y = y;
        Anchor.Height = height;
        Anchor.SuperView?.Layout();
        app.ToolTips?.ShowFor(Anchor, new ToolTipProvider(() => ToolTips.View(text)));
    }

    /// <summary>What the pointer rests on, so a tooltip is shown once per header or row it enters.</summary>
    private abstract record PointerOver
    {
        public sealed record Nothing : PointerOver;

        public sealed record Header(int Column) : PointerOver;

        public sealed record Row(int Index) : PointerOver;
    }
}