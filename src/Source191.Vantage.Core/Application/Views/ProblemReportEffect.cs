using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public abstract record ProblemReportEffect
{
    /// <summary>Nothing: the dialog pushes the new state back to its controls.</summary>
    public sealed record None : ProblemReportEffect;

    public sealed record Open(ReportTitle Title) : ProblemReportEffect;

    public sealed record Copy(ReportTitle Title) : ProblemReportEffect;

    public sealed record Close : ProblemReportEffect;
}