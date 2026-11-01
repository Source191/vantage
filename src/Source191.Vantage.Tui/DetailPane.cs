using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Tui;

/// <summary>
/// The pane under a tab's table: a grey divider titled with the selected row's name and the row's detail beneath it,
/// laid out by <see cref="DetailLayout"/> at the pane's width. It is as tall as the tab's detail can be, so the table's
/// height never changes with the selection; the tab places it above the status line.
/// </summary>
internal sealed class DetailPane : View
{
    private DetailContent content = DetailContent.Empty;

    /// <param name="lines">The most lines the tab's detail takes under the divider.</param>
    public DetailPane(int lines)
    {
        Rows = lines + 1;
        CanFocus = false;
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        SetScheme(Palette.TabFrame());
        X = 0;
        Width = Dim.Fill();
        Height = Rows;
    }

    /// <summary>The pane's height, divider included.</summary>
    public int Rows { get; }

    /// <summary>Shows a row's detail, or <see cref="DetailContent.Empty"/> for none: its divider then has no title, not two stray spaces.</summary>
    public void Show(DetailContent shown)
    {
        content = shown;
        Title = shown.Title.Length > 0 ? $" {shown.Title} " : string.Empty;
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
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