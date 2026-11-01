using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Opens tabs through wt.exe, run by the full path it was given, each tab running pwsh.exe by its full path (either
/// missing when it is not on the PATH, which fails each open with a sentence naming it). When Vantage's fragment is installed the tab asks for the "Vantage session"
/// profile, so its bell sounds different from other tabs; the file is checked on each open, so a setup run while
/// Vantage is open takes effect at once.
/// </summary>
public sealed class WindowsTerminal(IProcessRunner processes, ITerminalFragment fragment, ProgramLocation wt, ProgramLocation pwsh) : IWindowsTerminal
{
    public async Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        string executable = wt switch
        {
            ProgramLocation.Found found => found.Path,
            ProgramLocation.Missing => throw new TerminalException("Windows Terminal (wt.exe) was not found on the PATH, so no tab can be opened."),
            _ => throw new UnreachableException(),
        };
        string shell = pwsh switch
        {
            ProgramLocation.Found found => found.Path,
            ProgramLocation.Missing => throw new TerminalException("PowerShell 7 (pwsh.exe) was not found on the PATH, so no tab can be opened."),
            _ => throw new UnreachableException(),
        };

        string[] profile = fragment.Exists ? ["--profile", TerminalProfiles.SessionProfileName] : [];
        ProcessRequest request = new(executable, WindowsTerminalArguments.ForTab(launch, profile, shell), new WorkingFolder.At(launch.StartingDirectory.Value));
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new TerminalException($"Windows Terminal could not open the tab: {result.StandardError.Trim()}");
        }
    }

    public Task<bool> FocusTabAsync(TerminalWindow window, string title, CancellationToken cancellationToken) => Task.Run(() => UiAutomationTabs.Select(window, title), cancellationToken);
}