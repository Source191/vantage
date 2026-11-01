using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The problem report dialog's rules as a pure function of state and input; the dialog only binds controls to it. The
/// title is held as typed; a blank title becomes the default one, and a title that is too long is refused, live and
/// when opening or copying.
/// </summary>
public static class ProblemReportView
{
    public static ProblemReportUpdate Reduce(ProblemReportState state, ProblemReportInput input) => input switch
    {
        ProblemReportInput.TitleChanged changed => Changed(state with { TitleText = changed.Text, Problem = TooLong(changed.Text) }),
        ProblemReportInput.Open => Settled(state, title => new ProblemReportEffect.Open(title)),
        ProblemReportInput.Copy => Settled(state, title => new ProblemReportEffect.Copy(title)),
        ProblemReportInput.Cancel => new ProblemReportUpdate(state, new ProblemReportEffect.Close()),
        _ => Changed(state),
    };

    private static ProblemReportUpdate Settled(ProblemReportState state, Func<ReportTitle, ProblemReportEffect> effect)
    {
        if (string.IsNullOrWhiteSpace(state.TitleText))
        {
            return new ProblemReportUpdate(state, effect(state.DefaultTitle));
        }

        return ReportTitle.Validate(state.TitleText) switch
        {
            ReportTitleValidation.Valid valid => new ProblemReportUpdate(state with { Problem = new ReportTitleProblemLine.None() }, effect(valid.Title)),
            ReportTitleValidation.Refused refused => Changed(state with { Problem = new ReportTitleProblemLine.Shows(refused.Problem) }),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Only length is worth saying while typing: a blank box is allowed and takes the default title.</summary>
    private static ReportTitleProblemLine TooLong(string text) => text.Length > ReportTitle.MostCharacters ? new ReportTitleProblemLine.Shows(ReportTitleProblem.TooLong) : new ReportTitleProblemLine.None();

    private static ProblemReportUpdate Changed(ProblemReportState state) => new(state, new ProblemReportEffect.None());
}