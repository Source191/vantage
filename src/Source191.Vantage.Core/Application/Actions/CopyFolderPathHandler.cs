using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Puts a row's full folder path on the clipboard as plain text.</summary>
public sealed class CopyFolderPathHandler(IClipboard clipboard) : IEffectHandler<CopyFolderPath>
{
    public Task<Outcome> HandleAsync(CopyFolderPath effect, CancellationToken cancellationToken)
    {
        string path = effect.Folder.Value;
        Outcome outcome = clipboard.TrySetText(path) ? new FolderPathCopied(path) : new ClipboardBusy();

        return Task.FromResult(outcome);
    }
}