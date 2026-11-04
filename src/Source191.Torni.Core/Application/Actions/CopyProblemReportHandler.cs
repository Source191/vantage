using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>Puts the report's title and its whole text, untrimmed, on the clipboard; the crash file is left for a report that opens.</summary>
public sealed class CopyProblemReportHandler(IClipboard clipboard) : IEffectHandler<CopyProblemReport>
{
    public Task<Outcome> HandleAsync(CopyProblemReport effect, CancellationToken cancellationToken)
    {
        Outcome outcome = clipboard.TrySetText($"{effect.Title.Value}\n\n{effect.Report.Text}") ? new ProblemReportOutcome.Copied() : new ClipboardBusy();

        return Task.FromResult(outcome);
    }
}