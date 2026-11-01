using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Text;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Builds the <see href="https://learn.microsoft.com/windows/terminal/command-line-arguments">wt.exe argument list</see>.
/// wt.exe splits its command line on a semicolon even inside a quoted argument, so the tab's command travels as an
/// encoded PowerShell command and every semicolon left, in a title or a folder, is escaped.
/// </summary>
public static class WindowsTerminalArguments
{
    /// <summary>The arguments for a new tab; the profile's arguments are <c>--profile</c> and its name, or none for the default profile.</summary>
    public static IReadOnlyList<string> ForTab(TabLaunch launch, IReadOnlyList<string> profile)
    {
        string[] arguments =
        [
            "-w", "0", "new-tab",
            .. profile,
            "--title", launch.Title,
            "--tabColor", launch.Colour.Hex,
            "--suppressApplicationTitle",
            "--startingDirectory", launch.StartingDirectory.Value,
            "pwsh.exe", "-NoExit", "-EncodedCommand", EncodedCommand.Encode(launch.Script),
        ];

        return [.. arguments.Select(Escaped)];
    }

    /// <summary>wt.exe reads <c>\;</c> as a literal semicolon and leaves every other backslash as it is.</summary>
    private static string Escaped(string argument) => argument.Replace(";", @"\;", StringComparison.Ordinal);
}

/// <summary>
/// The <see href="https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_pwsh">-EncodedCommand
/// form PowerShell takes</see>: the script as Base64 of its UTF-16 bytes.
/// </summary>
public static class EncodedCommand
{
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
}

/// <summary>
/// Opens tabs through wt.exe, run by the full path it was given (missing when wt.exe is not on the PATH, which fails each
/// open with a sentence naming it). When Vantage's fragment is installed the tab asks for the "Vantage session"
/// profile, so its bell sounds different from other tabs; the file is checked on each open, so a setup run while
/// Vantage is open takes effect at once.
/// </summary>
public sealed class WindowsTerminal(IProcessRunner processes, ITerminalFragment fragment, ProgramLocation wt) : IWindowsTerminal
{
    public async Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        string executable = wt switch
        {
            ProgramLocation.Found found => found.Path,
            ProgramLocation.Missing => throw new TerminalException("Windows Terminal (wt.exe) was not found on the PATH, so no tab can be opened."),
            _ => throw new UnreachableException(),
        };

        string[] profile = fragment.Exists ? ["--profile", TerminalProfiles.SessionProfileName] : [];
        ProcessRequest request = new(executable, WindowsTerminalArguments.ForTab(launch, profile), new WorkingFolder.At(launch.StartingDirectory.Value));
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new TerminalException($"Windows Terminal could not open the tab: {result.StandardError.Trim()}");
        }
    }

    public Task<bool> FocusTabAsync(TerminalWindow window, string title, CancellationToken cancellationToken) => Task.Run(() => UiAutomationTabs.Select(window, title), cancellationToken);
}