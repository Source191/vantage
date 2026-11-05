namespace Source191.Torni.Tui;

/// <summary>How a table column takes its width: its natural width, cut at a number of cells, the width the others leave, or right-aligned.</summary>
internal abstract record ColumnFit
{
    /// <summary>The column is as wide as its widest cell.</summary>
    public sealed record Natural : ColumnFit;

    /// <summary>A cell is cut at this many cells with an ellipsis, so one long value cannot push the other columns off screen.</summary>
    public sealed record Capped(int Cells) : ColumnFit;

    /// <summary>The column takes the width the others leave; its definition fits the cell to the width it is handed.</summary>
    public sealed record Elastic : ColumnFit;

    /// <summary>The column is as wide as its widest cell and its cells end flush right, so durations line up on their last character.</summary>
    public sealed record RightAligned : ColumnFit;
}