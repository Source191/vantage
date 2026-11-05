using System.Diagnostics;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>Styles one Terminal.Gui table column from its <see cref="ColumnFit"/>.</summary>
internal static class ColumnFits
{
    /// <summary>A capped column is cut at its cells with an ellipsis and a right-aligned one ends flush right; the natural and elastic widths are the table's own and set nothing here.</summary>
    public static void Apply(ColumnStyle style, ColumnFit fit)
    {
        switch (fit)
        {
            case ColumnFit.Capped capped:
                style.MaxWidth = capped.Cells;
                style.TruncationIndicator = "…";
                break;
            case ColumnFit.RightAligned:
                style.Alignment = Alignment.End;
                break;
            case ColumnFit.Natural:
            case ColumnFit.Elastic:
                break;
            default:
                throw new UnreachableException();
        }
    }
}