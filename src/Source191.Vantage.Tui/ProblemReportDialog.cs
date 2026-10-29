using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The problem report dialog: what the report holds and where it goes, a title box, and a read-only preview of exactly
/// the diagnostics the page will be filled in with, bound to <see cref="ProblemReportView"/>, which holds every rule.
/// Nothing is opened or copied here; the flow dispatches the effect it returns.
/// </summary>
internal static class ProblemReportDialog
{
    /// <summary>Open or Copy with the title to use, or Close when the dialog was cancelled.</summary>
    public static ProblemReportEffect Show(IApplication app, DiagnosticLog log, ProblemReport report, ReportTitle defaultTitle)
    {
        ProblemReportState state = ProblemReportState.Open(defaultTitle);
        ProblemReportEffect result = new ProblemReportEffect.Close();
        bool binding = false;
        // TextView is marked obsolete in favour of the separate tui-cs Editor package; it is still the wrapping, scrolling box.
#pragma warning disable CS0618
        TextView explanation = new() { X = 1, Y = 0, Width = Dim.Fill(2), Height = 3, ReadOnly = true, WordWrap = true, CanFocus = false, Text = Wording.ProblemReportExplanation };
        TextView preview = new() { X = 1, Y = 7, Width = Dim.Fill(2), Height = Dim.Fill(2), ReadOnly = true, Text = report.Fitted };
#pragma warning restore CS0618
        TextField title = new() { X = 1, Y = 4, Width = Dim.Fill(2) };
        Label problem = new() { X = 1, Y = 5, Width = Dim.Fill(1), Height = 1, Text = " " };
        problem.SetScheme(Palette.Plain(Palette.Red));
        using Dialog dialog = new() { Title = Wording.ProblemReportDialogTitle, Width = Dim.Percent(90), Height = Dim.Percent(85) };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.ProblemReport);
        dialog.Add(explanation, Caption(3, Wording.ProblemReportTitleCaption), title, problem, Caption(6, Wording.ProblemReportPreviewCaption), preview);

        void Apply(ProblemReportInput input)
        {
            if (binding)
            {
                return;
            }

            ProblemReportUpdate update = ProblemReportView.Reduce(state, input);
            state = update.State;
            if (update.Effect is not ProblemReportEffect.None)
            {
                result = update.Effect;
                app.RequestStop(dialog);
                return;
            }

            binding = true;
            problem.Text = state.Problem is ReportTitleProblem refused ? Wording.Describe(refused) : " ";
            binding = false;
        }

        title.TextChanging += (_, changing) => Apply(new ProblemReportInput.TitleChanged(changing.Result ?? string.Empty));
        // Enter in the title box opens the report, as the default button would.
        title.Accepting += (_, accepting) => accepting.Handled = WizardControls.Handled(() => Apply(new ProblemReportInput.Open()));
        Button open = WizardControls.Button(Wording.ProblemReportButtons[0], () => Apply(new ProblemReportInput.Open()));
        open.IsDefault = true;
        dialog.AddButton(open);
        dialog.AddButton(WizardControls.Button(Wording.ProblemReportButtons[1], () => Apply(new ProblemReportInput.Copy())));
        dialog.AddButton(WizardControls.Button(Wording.ProblemReportButtons[2], () => Apply(new ProblemReportInput.Cancel())));
        dialog.Initialized += (_, _) => title.SetFocus();

        app.Run(dialog);

        return result;
    }

    private static Label Caption(int y, string text)
    {
        Label label = new() { X = 1, Y = y, Text = text };
        label.SetScheme(Palette.Plain(Palette.Orange));

        return label;
    }
}