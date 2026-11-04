using Source191.Torni.Adapters.Claude;
using Source191.Torni.Adapters.Files;
using Source191.Torni.Adapters.Terminal;
using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Launch;
using Source191.Torni.Domain;

namespace Source191.Torni.Composition;

/// <summary><c>torni here</c>: starts a session in the current folder and attaches this terminal to it.</summary>
internal static class HereComposition
{
    public static async Task<int> RunAsync(SharedParts parts, LaunchParts launch, HereName name, string prompt, CancellationToken cancellationToken)
    {
        ClaudeCli claude = launch.Claude;
        LaunchService launchService = launch.Launches;
        // here does not ask Claude Code for its version, so as not to slow the attach down.
        parts.RecordStart(new ClaudeVersionReport.NotRead());
        TerminalConsole hereConsole = new(Console.Out, Wording.TabTitle);
        HereCommandRunner runner = new(new Folders(), launchService, new TerminalEffects(claude, hereConsole), hereConsole, Console.Out, Console.Error);

        return await runner.RunAsync(Directory.GetCurrentDirectory(), name, prompt, cancellationToken);
    }
}