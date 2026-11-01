namespace Source191.Vantage.Application.Reporting;

/// <summary>One line of a report and its place in the order lines are left out; <see cref="Never"/> is always kept.</summary>
internal sealed record ReportLine(string Text, int Drop)
{
    public const int Never = int.MaxValue;
}