using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using System.Text;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>A tab to open: what it shows, where it starts, and the PowerShell script it runs.</summary>
public sealed record TabLaunch(string Title, TabColour Colour, string StartingDirectory, string Script);

public sealed class TerminalException(string message) : Exception(message);

public interface IWindowsTerminal
{
    Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken);

    /// <summary>
    /// Selects the tab titled <paramref name="title"/> in the window of the given Windows Terminal process, or in any
    /// Windows Terminal window when the process is unknown. False when no such tab item could be found.
    /// </summary>
    Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken);
}

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
            "--startingDirectory", launch.StartingDirectory,
            "pwsh.exe", "-NoExit", "-EncodedCommand", PowerShellScript.Encode(launch.Script),
        ];
    }
}

public static class PowerShellScript
{
    public static string Quote(string text)
    {
        return $"'{text.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    public static string Invoke(string executable, IReadOnlyList<string> arguments)
    {
        IEnumerable<string> quoted = new[] { executable }.Concat(arguments).Select(Quote);

        return $"& {string.Join(' ', quoted)}";
    }

    public static string Encode(string script)
    {
        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }
}

public sealed class WindowsTerminal(IProcessRunner processes) : IWindowsTerminal
{
    public async Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        ProcessRequest request = new("wt.exe", WindowsTerminalArguments.ForTab(launch), launch.StartingDirectory);
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new TerminalException($"Windows Terminal could not open the tab: {result.StandardError.Trim()}");
        }
    }

    public Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken)
    {
        return Task.Run(() => UiAutomationTabs.Select(terminalProcessId, title), cancellationToken);
    }
}