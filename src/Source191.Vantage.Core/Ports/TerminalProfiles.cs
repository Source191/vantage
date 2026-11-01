namespace Source191.Vantage.Ports;

/// <summary>
/// The two Windows Terminal profiles Vantage adds, named once: "Vantage" runs the installed Vantage, "Vantage session"
/// is PowerShell and, once the fragment exists, the profile every tab Vantage opens asks for. Each has its bell sound,
/// a file or Windows Terminal's own.
/// </summary>
public sealed record TerminalProfiles(string VantageExecutable, BellSound VantageSound, BellSound SessionSound)
{
    public const string VantageProfileName = "Vantage";

    public const string SessionProfileName = "Vantage session";

    public const string SessionCommandLine = "pwsh.exe -NoLogo";

    /// <summary>
    /// <see href="https://learn.microsoft.com/windows/terminal/customize-settings/profile-advanced">Sound, flash the window,
    /// flash the taskbar</see>.
    /// </summary>
    public static IReadOnlyList<string> BellStyle { get; } = ["audible", "window", "taskbar"];
}