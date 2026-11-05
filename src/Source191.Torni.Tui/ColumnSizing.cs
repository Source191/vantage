using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>
/// A table's columns as the screen draws them: each column's style from its own fit, the elastic column's width from what the
/// others need, the column offset the <c>&lt;</c> and <c>&gt;</c> keys scroll, and the colours the per-cell and per-row getters
/// read off the selected row (set once per redraw). It owns the styles of the one <see cref="TableView"/> it is given.
/// </summary>
internal sealed class ColumnSizing<TRow>
{
    private const int ElasticFloor = 20;

    private readonly TableView table;
    private readonly IReadOnlyList<TableColumn<TRow>> columns;
    private readonly Func<TRow, Color> rowColour;
    private readonly IApplication app;
    private readonly string[] columnNames;
    private readonly int elasticColumn;
    private TRow? selected;
    private int columnOffset;

    public ColumnSizing(TableView table, IReadOnlyList<TableColumn<TRow>> columns, Func<TRow, Color> rowColour, IApplication app)
    {
        this.table = table;
        this.columns = columns;
        this.rowColour = rowColour;
        this.app = app;
        columnNames = [.. columns.Select(column => column.Help.Name)];
        elasticColumn = IndexOfElastic(columns);
        table.Style.RowColorGetter = RowColour;
        StyleColumns();
    }

    /// <summary>The row the colour getters draw highlighted, kept so they do not recompute the visible rows per cell.</summary>
    public void Selected(TRow? row) => selected = row;

    /// <summary>The rows as the table's source, the elastic column's cells fitted to the width it has left.</summary>
    public TableSource<TRow> Source(IReadOnlyList<TRow> rows, DateTimeOffset now)
    {
        int elasticWidth = ElasticWidth(rows, now);

        return new TableSource<TRow>(rows, columnNames, (row, column) => columns[column].Cell(row, now, column == elasticColumn ? elasticWidth : int.MaxValue));
    }

    /// <summary>Puts the kept offset back: the table loses its offset on every source swap.</summary>
    public void RestoreOffset() => table.ColumnOffset = columnOffset;

    public void Scroll(int step)
    {
        // Kept here as well as on the table, because every redraw replaces the table's source, which resets its offset.
        columnOffset = Math.Clamp(columnOffset + step, 0, columnNames.Length - 1);
        table.ColumnOffset = columnOffset;
        table.SetNeedsDraw();
    }

    /// <summary>
    /// Each column is styled from its own fit: a capped one is cut at its cells with an ellipsis, so one long value cannot push the other
    /// columns off screen (rows keep the full values), and a right-aligned one ends flush right. Every column asks for a cell's own paint.
    /// </summary>
    private void StyleColumns()
    {
        for (int column = 0; column < columnNames.Length; column++)
        {
            ColumnStyle style = table.Style.GetOrCreateColumnStyle(column);
            style.ColorGetter = CellColour;
            ColumnFits.Apply(style, columns[column].Fit);
        }
    }

    /// <summary>The one column whose fit is elastic; every view has exactly one, which its definition's fact keeps.</summary>
    private static int IndexOfElastic(IReadOnlyList<TableColumn<TRow>> columns)
    {
        for (int column = 0; column < columns.Count; column++)
        {
            if (columns[column].Fit is ColumnFit.Elastic)
            {
                return column;
            }
        }

        throw new InvalidOperationException("A table view needs one elastic column.");
    }

    /// <summary>
    /// The cells the elastic column may have: the table's content width minus the other columns' widest visible cells (capped as the
    /// styles cap them) and a separator per column, never below the floor. The elastic column's style is held to it, both ways.
    /// </summary>
    private int ElasticWidth(IReadOnlyList<TRow> rows, DateTimeOffset now)
    {
        int contentWidth = table.Viewport.Width > 0 ? table.Viewport.Width : app.Screen.Width - 2;
        int others = 0;
        for (int column = 0; column < columnNames.Length; column++)
        {
            if (column != elasticColumn)
            {
                others += WidestCell(rows, column, now);
            }
        }

        // The table sizes a column to its content, so the minimum is what makes the elastic column take the spare width.
        int width = Math.Max(contentWidth - others - columnNames.Length - 1, ElasticFloor);
        ColumnStyle style = table.Style.GetOrCreateColumnStyle(elasticColumn);
        style.MaxWidth = width;
        style.MinWidth = width;

        return width;
    }

    private int WidestCell(IReadOnlyList<TRow> rows, int column, DateTimeOffset now)
    {
        int widest = Math.Max(columnNames[column].Length, rows.Count == 0 ? 0 : rows.Max(row => columns[column].Cell(row, now, int.MaxValue).Length));
        ColumnStyle? style = table.Style.GetColumnStyleIfAny(column);

        return style?.MaxWidth is int cap ? Math.Min(widest, cap) : widest;
    }

    /// <summary>A cell's own paint: a fill (the TAB swatch) shows on every row, the selected one included; a foreground colour only off the selected row, which keeps its highlight.</summary>
    private Scheme? CellColour(CellColorGetterArgs args)
    {
        if (args.Table is not TableSource<TRow> source)
        {
            return null;
        }

        TRow row = source.RowAt(args.RowIndex);
        bool onSelected = Equals(row, selected);

        // Null is Terminal.Gui's "the row's own scheme applies".
        return columns[args.ColIdex].Paint(row) switch
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
            : Palette.Plain(rowColour(row));
    }
}