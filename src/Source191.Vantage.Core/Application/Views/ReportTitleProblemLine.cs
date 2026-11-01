using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>What the problem report dialog shows under the title: nothing, or why the title is refused.</summary>
public abstract record ReportTitleProblemLine
{
    /// <summary>Nothing is wrong: the problem line is blank.</summary>
    public sealed record None : ReportTitleProblemLine;

    public sealed record Shows(ReportTitleProblem Problem) : ReportTitleProblemLine;
}