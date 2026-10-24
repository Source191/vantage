using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Opens a plain PowerShell tab in a worktree, in the last palette colour so it never looks like a session.</summary>
public sealed class OpenShellHandler(IWindowsTerminal terminal) : IEffectHandler<OpenShellTab>
{
    private static readonly TabColour ShellTabColour = TabPalette.Default[^1];

    public async Task<string> HandleAsync(OpenShellTab effect, CancellationToken cancellationToken)
    {
        WorktreeRow row = effect.Row;
        await terminal.OpenTabAsync(new TabLaunch(row.FolderName, ShellTabColour, row.Path, "Clear-Host"), cancellationToken);

        return $"Opened a shell in {row.FolderName}.";
    }
}