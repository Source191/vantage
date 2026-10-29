using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Opens the repository's problem report form in the browser, filled in with the title and the report fitted into the
/// address. Vantage sends nothing itself. A crash the report carried is removed once the page has opened, so the next
/// report does not repeat it; when the browser cannot be started the crash stays and the words suggest copying instead.
/// </summary>
public sealed class OpenProblemReportHandler(IDesktop desktop, ICrashFile crashFile) : IEffectHandler<OpenProblemReport>
{
    public async Task<Outcome> HandleAsync(OpenProblemReport effect, CancellationToken cancellationToken)
    {
        // The previewed text, unless this title escapes longer than the room it leaves.
        string fitted = effect.Report.Fitted;
        if (ProblemReportAddress.Length(effect.Title, fitted) > ProblemReport.AddressLimit)
        {
            fitted = effect.Report.FitWithin(effect.Title, ProblemReport.AddressLimit);
        }

        try
        {
            await desktop.OpenAsync(ProblemReportAddress.For(effect.Title, fitted).AbsoluteUri, cancellationToken);
        }
        catch (InvalidOperationException failure)
        {
            return new ProblemReportOutcome.NotOpened(failure.Message);
        }

        if (effect.Report.HasCrash)
        {
            await crashFile.RemoveAsync();
        }

        return new ProblemReportOutcome.Opened();
    }
}