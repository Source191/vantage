using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Builds the wt.exe argument list. The tab's command travels as an encoded PowerShell command because wt.exe splits
/// its command line on a semicolon even inside a quoted argument; the title is a session name, which
/// <c>SessionName</c> keeps free of semicolons.
/// </summary>
public static class WindowsTerminalArguments
{
    /// <summary>The arguments for a new tab; a profile name adds <c>--profile</c>, null leaves the tab in the default profile.</summary>
    public static IReadOnlyList<string> ForTab(TabLaunch launch, string? profile)
    {
        return
        [
            "-w", "0", "new-tab",
            .. profile is null ? (string[])[] : ["--profile", profile],
            "--title", launch.Title,
            "--tabColor", launch.Colour.Hex,
            "--suppressApplicationTitle",
            "--startingDirectory", launch.StartingDirectory.Value,
            "pwsh.exe", "-NoExit", "-EncodedCommand", EncodedCommand.Encode(launch.Script),
        ];
    }
}

/// <summary>The -EncodedCommand form PowerShell takes: the script as Base64 of its UTF-16 bytes.</summary>
public static class EncodedCommand
{
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
}

/// <summary>
/// Opens tabs through wt.exe. When Vantage's fragment is installed the tab asks for the "Vantage session" profile, so
/// its bell sounds different from other tabs; the file is checked on each open, so a setup run while Vantage is open
/// takes effect at once.
/// </summary>
public sealed class WindowsTerminal(IProcessRunner processes, ITerminalFragment fragment) : IWindowsTerminal
{
    public async Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        string? profile = fragment.Exists ? TerminalProfiles.SessionProfileName : null;
        ProcessRequest request = new("wt.exe", WindowsTerminalArguments.ForTab(launch, profile), launch.StartingDirectory.Value);
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new TerminalException($"Windows Terminal could not open the tab: {result.StandardError.Trim()}");
        }
    }

    public Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken) => Task.Run(() => UiAutomationTabs.Select(terminalProcessId, title), cancellationToken);
}