using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Builds the <see href="https://learn.microsoft.com/windows/terminal/command-line-arguments">wt.exe argument list</see>.
/// wt.exe splits its command line on a semicolon even inside a quoted argument, so the tab's command travels as an
/// encoded PowerShell command and every semicolon left, in a title or a folder, is escaped.
/// </summary>
public static class WindowsTerminalArguments
{
    /// <summary>
    /// The arguments for a new tab; the profile's arguments are <c>--profile</c> and its name, or none for the default
    /// profile. PowerShell is named by its full path: a bare <c>pwsh.exe</c> is resolved by Windows Terminal's own
    /// <c>CreateProcess</c>, which on a cold start can search the caller's current folder first.
    /// </summary>
    public static IReadOnlyList<string> ForTab(TabLaunch launch, IReadOnlyList<string> profile, string pwsh)
    {
        string[] arguments =
        [
            "-w", "0", "new-tab",
            .. profile,
            "--title", launch.Title,
            "--tabColor", launch.Colour.Hex,
            "--suppressApplicationTitle",
            "--startingDirectory", launch.StartingDirectory.Value,
            pwsh, "-NoExit", "-EncodedCommand", EncodedCommand.Encode(launch.Script),
        ];

        return [.. arguments.Select(Escaped)];
    }

    /// <summary>wt.exe reads <c>\;</c> as a literal semicolon and leaves every other backslash as it is.</summary>
    private static string Escaped(string argument) => argument.Replace(";", @"\;", StringComparison.Ordinal);
}