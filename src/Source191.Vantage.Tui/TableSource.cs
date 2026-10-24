using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>Presents a view's visible rows to Terminal.Gui's TableView through the definition's columns and cells.</summary>
internal sealed class TableSource<TRow>(IReadOnlyList<TRow> rows, IReadOnlyList<string> columnNames, Func<TRow, int, string> cell) : ITableSource
{
    private readonly string[] names = [.. columnNames];

    public int Rows => rows.Count;

    public int Columns => names.Length;

    public string[] ColumnNames => names;

    public object this[int row, int col] => cell(rows[row], col);

    public TRow RowAt(int index)
    {
        return rows[index];
    }
}