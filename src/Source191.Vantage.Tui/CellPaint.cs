using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>A cell's own colour; <paramref name="Always"/> keeps it on the selected row too (the TAB block), where every other cell takes the highlight.</summary>
internal sealed record CellPaint(Color Foreground, bool Always = false);