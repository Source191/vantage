using Source191.Torni.Application.Reporting;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Puts the title and the whole report on the clipboard instead.</summary>
public sealed record CopyProblemReport(ReportTitle Title, ProblemReport Report) : ViewEffect;