using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Application.TerminalSetup;

namespace Source191.Vantage.Composition;

/// <summary><c>vantage setup-terminal</c>: needs neither Claude Code nor a terminal tab, so Claude Code is not located.</summary>
internal static class TerminalSetupComposition
{
    public static async Task<int> RunAsync(SharedParts parts, TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        string windowsMedia = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
        TerminalSetupRunner setupRunner = new(new Folders(), parts.TerminalFragment, ClaudeSettings.ForCurrentUser(), parts.VantageExecutable, windowsMedia, Console.In, Console.Out, Console.Error);

        return await setupRunner.RunAsync(request, cancellationToken);
    }
}