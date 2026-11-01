using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>A cell's own paint: none (the row's colour), a text colour off the selected row only, or a fill kept on the selected row too (the TAB block).</summary>
internal abstract record CellPaint
{
    /// <summary>The cell takes its row's colour, and the highlight on the selected row.</summary>
    public sealed record None : CellPaint;

    /// <summary>The cell's own colour off the selected row; the selected row's highlight covers it (the grey hollow block).</summary>
    public sealed record Text(Color Foreground) : CellPaint;

    /// <summary>The cell's own colour on every row, the selected one included (the TAB swatch).</summary>
    public sealed record Fill(Color Foreground) : CellPaint;
}