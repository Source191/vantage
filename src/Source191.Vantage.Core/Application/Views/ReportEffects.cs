using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The <c>!</c> key in either view: the problem report dialog, which builds the report and shows what it holds.</summary>
public sealed record ShowProblemReport : ViewEffect, IDialogEffect;

/// <summary>Opens the issue form in the browser with the report fitted into its address; the title never reaches the diagnostic log.</summary>
public sealed record OpenProblemReport(ReportTitle Title, ProblemReport Report) : ViewEffect;

/// <summary>Puts the title and the whole report on the clipboard instead.</summary>
public sealed record CopyProblemReport(ReportTitle Title, ProblemReport Report) : ViewEffect;