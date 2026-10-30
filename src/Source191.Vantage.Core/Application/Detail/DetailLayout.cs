namespace Source191.Vantage.Application.Detail;

/// <summary>
/// Sets the detail pane's content on its rows: the facts in three columns, each as wide as its widest one-column fact
/// (labels orange, padded to the column's longest label) with the spare width going to the last, the widest column
/// narrowed first when the pane is too narrow and a cut fact ending in an ellipsis; then the lines beneath, from their
/// start or, for a screen, their end, as many as the rows left hold.
/// </summary>
public static class DetailLayout
{
    private const int Columns = 3;
    private const int Gap = 2;

    public static IReadOnlyList<IReadOnlyList<TextPart>> Rows(DetailContent content, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(content);
        IReadOnlyList<IReadOnlyList<DetailLine>> facts = [.. content.Facts.Take(Math.Max(height, 0))];
        int[] labels = LabelWidths(content.Facts);
        int[] widths = ColumnWidths(content.Facts, labels, width);
        IReadOnlyList<DetailLine> below = content.Below ?? [];
        int room = Math.Max(height - facts.Count, 0);
        IEnumerable<DetailLine> shown = content.KeepsLastLines ? below.TakeLast(room) : below.Take(room);
        int belowLabel = below.Any(line => line.Label.Length > 0) ? below.Max(line => line.Label.Length) + 1 : 0;

        return [.. facts.Select(row => FactRow(row, labels, widths)), .. shown.Select(line => Cell(line, belowLabel, width))];
    }

    private static List<TextPart> FactRow(IReadOnlyList<DetailLine> row, int[] labels, int[] widths)
    {
        List<TextPart> parts = [];
        int column = 0;
        foreach (DetailLine line in row)
        {
            if (column > 0)
            {
                parts.Add(new TextPart(new string(' ', Gap), TextTone.Plain));
            }

            int last = Math.Min(column + line.Span, Columns) - 1;
            int cellWidth = widths[column..(last + 1)].Sum() + (Gap * (last - column));
            parts.AddRange(Cell(line, labels[column], cellWidth));
            column = last + 1;
        }

        return parts;
    }

    private static TextPart[] Cell(DetailLine line, int labelWidth, int width)
    {
        int room = Math.Max(width - labelWidth, 0);
        string text = line.Text.Length <= room ? line.Text : line.Text[..Math.Max(room - 1, 0)] + "…";

        return labelWidth == 0
            ? [new TextPart(text.PadRight(room), line.Tone)]
            : [new TextPart(line.Label.PadRight(labelWidth), TextTone.Orange), new TextPart(text.PadRight(room), line.Tone)];
    }

    /// <summary>Each column's label width: its longest label and a space.</summary>
    private static int[] LabelWidths(IReadOnlyList<IReadOnlyList<DetailLine>> facts)
    {
        int[] labels = new int[Columns];
        foreach ((DetailLine line, int column) in Placed(facts))
        {
            labels[column] = Math.Max(labels[column], line.Label.Length + 1);
        }

        return labels;
    }

    /// <summary>Each column as wide as its widest one-column fact; the widest narrowed a cell at a time while the row is too wide, and the spare width given to the last.</summary>
    private static int[] ColumnWidths(IReadOnlyList<IReadOnlyList<DetailLine>> facts, int[] labels, int width)
    {
        int[] widths = [.. labels.Select(label => label + 1)];
        foreach ((DetailLine line, int column) in Placed(facts).Where(placed => placed.Line.Span == 1))
        {
            widths[column] = Math.Max(widths[column], labels[column] + line.Text.Length);
        }

        int available = width - (Gap * (Columns - 1));
        while (widths.Sum() > available && Narrowable(widths, labels) is int widest)
        {
            widths[widest]--;
        }

        widths[^1] += Math.Max(available - widths.Sum(), 0);

        return widths;
    }

    /// <summary>The first of the widest columns that still has room for a character after its label.</summary>
    private static int? Narrowable(int[] widths, int[] labels)
    {
        int[] candidates = [.. Enumerable.Range(0, Columns).Where(column => widths[column] > labels[column] + 1)];

        return candidates.Length == 0 ? null : candidates.MaxBy(column => widths[column]);
    }

    private static IEnumerable<(DetailLine Line, int Column)> Placed(IReadOnlyList<IReadOnlyList<DetailLine>> facts)
    {
        foreach (IReadOnlyList<DetailLine> row in facts)
        {
            int column = 0;
            foreach (DetailLine line in row.TakeWhile(_ => column < Columns))
            {
                yield return (line, column);
                column += line.Span;
            }
        }
    }
}
