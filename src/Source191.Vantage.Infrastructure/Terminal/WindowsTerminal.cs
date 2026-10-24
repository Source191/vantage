using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Builds the wt.exe argument list. The tab's command travels as an encoded PowerShell command because
/// Windows Terminal splits its command line on semicolons and the title is user text.
/// </summary>
public static class WindowsTerminalArguments
{
    public static IReadOnlyList<string> ForTab(TabLaunch launch)
    {
        return
        [
            "-w", "0", "new-tab",
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

public sealed class WindowsTerminal(IProcessRunner processes) : IWindowsTerminal
{
    public async Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        ProcessRequest request = new("wt.exe", WindowsTerminalArguments.ForTab(launch), launch.StartingDirectory.Value);
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new TerminalException($"Windows Terminal could not open the tab: {result.StandardError.Trim()}");
        }
    }

    public Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken) => Task.Run(() => UiAutomationTabs.Select(terminalProcessId, title), cancellationToken);
}