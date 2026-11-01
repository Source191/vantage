using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The dialog's state: the title as typed, the problem to show under it, and the title a blank box stands for.</summary>
public sealed record ProblemReportState(string TitleText, ReportTitleProblemLine Problem, ReportTitle DefaultTitle)
{
    public static ProblemReportState Open(ReportTitle defaultTitle) => new(string.Empty, new ReportTitleProblemLine.None(), defaultTitle);
}