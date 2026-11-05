using Source191.Torni.Application;

namespace Source191.Torni.Tui;

/// <summary>
/// One column of a table view: its help from <see cref="Wording"/> (the name that heads it, the tooltip and the help),
/// how its cell is written, how it fits and its cell's own paint.
/// </summary>
/// <param name="Help">The column's name and meaning.</param>
/// <param name="Cell">A cell's text; the int is the cells the elastic column has, and int.MaxValue for every other column.</param>
/// <param name="Fit">How the column takes its width.</param>
/// <param name="Paint">One cell's own paint (the TAB swatch), or <see cref="CellPaint.None"/> for the row's colour.</param>
internal sealed record TableColumn<TRow>(ColumnHelp Help, Func<TRow, DateTimeOffset, int, string> Cell, ColumnFit Fit, Func<TRow, CellPaint> Paint)
{
    /// <summary>A column whose cells take their row's colour.</summary>
    public static TableColumn<TRow> Plain(ColumnHelp help, Func<TRow, DateTimeOffset, int, string> cell, ColumnFit fit) => new(help, cell, fit, _ => new CellPaint.None());
}