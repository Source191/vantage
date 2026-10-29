using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Reporting;

/// <summary>
/// The address of the repository's "Problem report" issue form, filled in with a title and the diagnostics: the one home
/// of the new-issue address and of the form's file name and field id, which <c>ProblemReportTemplateShould</c> checks
/// against <c>.github\ISSUE_TEMPLATE\problem-report.yml</c>.
/// </summary>
public static class ProblemReportAddress
{
    public const string TemplateFile = "problem-report.yml";
    public const string DiagnosticsField = "diagnostics";

    private const string NewIssue = "https://github.com/Source191/vantage/issues/new";

    public static Uri For(ReportTitle title, string diagnostics) => new(Text(title, diagnostics));

    /// <summary>The address's length as sent, without building a <see cref="Uri"/>, for fitting the diagnostics.</summary>
    public static int Length(ReportTitle title, string diagnostics) => Text(title, diagnostics).Length;

    private static string Text(ReportTitle title, string diagnostics)
    {
        ArgumentNullException.ThrowIfNull(title);

        return $"{NewIssue}?template={TemplateFile}&title={Uri.EscapeDataString(title.Value)}&{DiagnosticsField}={Uri.EscapeDataString(diagnostics)}";
    }
}