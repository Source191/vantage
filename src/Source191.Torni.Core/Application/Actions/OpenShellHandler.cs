using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>Opens a plain PowerShell tab in a worktree; <see cref="TabScripts.Shell"/> says what it runs and its colour.</summary>
public sealed class OpenShellHandler(IWindowsTerminal terminal) : IEffectHandler<OpenShellTab>
{
    public async Task<Outcome> HandleAsync(OpenShellTab effect, CancellationToken cancellationToken)
    {
        WorktreeRow row = effect.Row;
        await terminal.OpenTabAsync(TabScripts.Shell(row.Path), cancellationToken);

        return new OpenShellOutcome(row.FolderName);
    }
}