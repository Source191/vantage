using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Puts the title and the whole report on the clipboard instead.</summary>
public sealed record CopyProblemReport(ReportTitle Title, ProblemReport Report) : ViewEffect;