using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Tui;

/// <summary>
/// The pane under a tab's table: a grey divider titled with the selected row's name, which the mouse drags up and down,
/// and the row's detail beneath it, laid out by <see cref="DetailLayout"/> at whatever width and height the pane has.
/// A drag leaves Terminal.Gui's absolute frame behind; the pane takes the new height, anchors itself above the footer
/// again and says so, so the console can give the other tab the same height.
/// </summary>
internal sealed class DetailPane : View
{
    /// <summary>The divider and two lines; the table keeps its header and a row above.</summary>
    private const int FewestRows = 3;
    private const int TableRowsKept = 4;
    private const int FooterRows = 1;

    private DetailContent? content;
    private int rows;
    private bool anchoring;

    public DetailPane(int rows)
    {
        CanFocus = false;
        Arrangement = ViewArrangement.TopResizable;
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        SetScheme(Palette.TabFrame());
        Anchor(rows);
        FrameChanged += (_, _) => OnFrameChanged();
    }

    /// <summary>Raised with the pane's new height after the user drags its divider.</summary>
    public event Action<int>? Resized;

    /// <summary>The pane's height, divider included.</summary>
    public int Rows => rows;

    /// <summary>How many lines fit under the divider.</summary>
    public int Lines => Math.Max(rows - 1, 0);

    public void Show(DetailContent? shown)
    {
        content = shown;
        Title = shown is null ? string.Empty : $" {shown.Title} ";
        SetNeedsDraw();
    }

    /// <summary>Takes a height set elsewhere (the other tab's drag) without raising <see cref="Resized"/>.</summary>
    public void Take(int height)
    {
        if (height != rows)
        {
            Anchor(height);
        }
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

    private void OnFrameChanged()
    {
        if (anchoring || Frame.Height == rows)
        {
            return;
        }

        int most = SuperView is null ? Frame.Height : Math.Max(SuperView.Viewport.Height - FooterRows - TableRowsKept, FewestRows);
        Anchor(Math.Clamp(Frame.Height, FewestRows, most));
        Resized?.Invoke(rows);
    }

    private void Anchor(int height)
    {
        anchoring = true;
        rows = height;
        X = 0;
        Y = Pos.AnchorEnd(height + FooterRows);
        Width = Dim.Fill();
        Height = height;
        anchoring = false;
        SuperView?.SetNeedsLayout();
    }
}