namespace Source191.Torni.Ports;

/// <summary>
/// The two Windows Terminal profiles the console adds, named once: "Torni" runs the installed program, "Torni session"
/// is PowerShell and, once the fragment exists, the profile every tab the console opens asks for. Each has its bell sound,
/// a file or Windows Terminal's own.
/// </summary>
public sealed record TerminalProfiles(string ConsoleExecutable, BellSound ConsoleSound, BellSound SessionSound)
{
    public const string ConsoleProfileName = "Torni";

    public const string SessionProfileName = "Torni session";

    public const string SessionCommandLine = "pwsh.exe -NoLogo";

    /// <summary>
    /// <see href="https://learn.microsoft.com/windows/terminal/customize-settings/profile-advanced">Sound, flash the window,
    /// flash the taskbar</see>.
    /// </summary>
    public static IReadOnlyList<string> BellStyle { get; } = ["audible", "window", "taskbar"];
}