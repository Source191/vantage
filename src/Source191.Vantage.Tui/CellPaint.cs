using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>A cell's own colours: a foreground, and a fill that shows on every row, the selected one included (the TAB swatch).</summary>
internal sealed record CellPaint(Color Foreground, Color? Fill = null);