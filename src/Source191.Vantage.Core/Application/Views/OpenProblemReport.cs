using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Opens the issue form in the browser with the report fitted into its address; the title never reaches the diagnostic log.</summary>
public sealed record OpenProblemReport(ReportTitle Title, ProblemReport Report) : ViewEffect;