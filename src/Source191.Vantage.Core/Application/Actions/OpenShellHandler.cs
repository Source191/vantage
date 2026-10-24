using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Opens a plain PowerShell tab in a worktree, in the last palette colour so it never looks like a session.</summary>
public sealed class OpenShellHandler(IWindowsTerminal terminal) : IEffectHandler<OpenShellTab>
{
    private static readonly TabColour ShellTabColour = TabPalette.Default[^1];

    public async Task<Outcome> HandleAsync(OpenShellTab effect, CancellationToken cancellationToken)
    {
        WorktreeRow row = effect.Row;
        await terminal.OpenTabAsync(new TabLaunch(row.FolderName, ShellTabColour, row.Path, "Clear-Host"), cancellationToken);

        return new OpenShellOutcome(row.FolderName);
    }
}

/// <summary>A plain shell tab was opened in the worktree.</summary>
public sealed record OpenShellOutcome(string FolderName) : Outcome;