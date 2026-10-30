using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Tui;

/// <summary>
/// The pane under a tab's table: a grey divider titled with the selected row's name and the row's detail beneath it,
/// laid out by <see cref="DetailLayout"/> at the pane's width. It is as tall as the tab's detail can be, above the footer,
/// so the table's height never changes with the selection.
/// </summary>
internal sealed class DetailPane : View
{
    private const int FooterRows = 1;

    private DetailContent? content;

    /// <param name="lines">The most lines the tab's detail takes under the divider.</param>
    public DetailPane(int lines)
    {
        Rows = lines + 1;
        CanFocus = false;
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        SetScheme(Palette.TabFrame());
        X = 0;
        Y = Pos.AnchorEnd(Rows + FooterRows);
        Width = Dim.Fill();
        Height = Rows;
    }

    /// <summary>The pane's height, divider included.</summary>
    public int Rows { get; }

    public void Show(DetailContent? shown)
    {
        content = shown;
        Title = shown is null ? string.Empty : $" {shown.Title} ";
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        if (content is null)
        {
            return true;
        }

        int width = Math.Max(Viewport.Width - 2, 0);
        IReadOnlyList<IReadOnlyList<TextPart>> laid = DetailLayout.Rows(content, width, Viewport.Height);
        for (int row = 0; row < laid.Count; row++)
        {
            Move(1, row);
            foreach (TextPart part in laid[row])
            {
                SetAttribute(new Attribute(Palette.ForTone(part.Tone), Palette.Background));
                AddStr(part.Text);
            }
        }

        return true;
    }
}