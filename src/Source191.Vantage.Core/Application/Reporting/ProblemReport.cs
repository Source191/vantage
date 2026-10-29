using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Reporting;

/// <summary>
/// The diagnostics a problem report carries, built once so the dialog's preview and the opened page hold the same text.
/// Each line knows when it may be left out to fit the page's address: the environment, the headings and the crash
/// itself never; the crash file's earlier lines first, then ordinary events oldest first, then the counts of runs that
/// exited 0, then failures and the other counts oldest first.
/// </summary>
public sealed class ProblemReport
{
    /// <summary>Under GitHub's limit of about 8 KB, and under what Windows can pass to the browser once Desktop has encoded it.</summary>
    public const int AddressLimit = 7500;

    /// <summary>A title as long as one can be, of the ASCII character that escapes longest.</summary>
    private static readonly ReportTitle LongestTitle = ReportTitle.From(new string('%', ReportTitle.MostCharacters));

    private readonly IReadOnlyList<ReportLine> lines;

    internal ProblemReport(IReadOnlyList<ReportLine> lines, bool hasCrash)
    {
        this.lines = lines;
        HasCrash = hasCrash;
    }

    public bool HasCrash { get; }

    public string Text => Join(lines);

    /// <summary>
    /// The text the dialog previews and the page is filled in with: fitted with room for the longest title, so it does not
    /// change as the title is typed. Only a long title of characters outside ASCII can need more room.
    /// </summary>
    public string Fitted => FitWithin(LongestTitle, AddressLimit);

    /// <summary>The text with lines left out, in order, until the address for this title is at most <paramref name="limit"/> characters.</summary>
    public string FitWithin(ReportTitle title, int limit)
    {
        List<ReportLine> kept = [.. lines];
        Queue<ReportLine> droppable = new(lines.Where(line => line.Drop != ReportLine.Never).OrderBy(line => line.Drop));
        int dropped = 0;
        string text = Join(kept);
        while (ProblemReportAddress.Length(title, text) > limit && droppable.TryDequeue(out ReportLine? next))
        {
            kept.Remove(next);
            dropped++;
            text = Join(kept) + LeftOut(dropped);
        }

        return text;
    }

    private static string Join(IEnumerable<ReportLine> lines) => string.Join('\n', lines.Select(line => line.Text));

    private static string LeftOut(int dropped) => dropped == 1 ? "\n(1 line left out to fit the page address)" : $"\n({dropped} lines left out to fit the page address)";
}

/// <summary>One line of a report and its place in the order lines are left out; <see cref="Never"/> is always kept.</summary>
internal sealed record ReportLine(string Text, int Drop)
{
    public const int Never = int.MaxValue;
}