namespace Source191.Vantage.Ports;

/// <summary>
/// The two Windows Terminal profiles Vantage adds, named once: "Vantage" runs the installed Vantage, "Vantage session"
/// is PowerShell and is the profile every tab Vantage opens asks for. A null sound leaves that profile on Windows
/// Terminal's own bell sound.
/// </summary>
public sealed record TerminalProfiles(string VantageExecutable, string? VantageSound, string? SessionSound)
{
    public const string VantageProfileName = "Vantage";

    public const string SessionProfileName = "Vantage session";

    public const string SessionCommandLine = "pwsh.exe -NoLogo";

    /// <summary>Sound, flash the window, flash the taskbar.</summary>
    public static IReadOnlyList<string> BellStyle { get; } = ["audible", "window", "taskbar"];
}

/// <summary>
/// The Windows Terminal JSON fragment that carries Vantage's profiles: a file Windows Terminal reads at start-up and
/// that the user's own settings file never sees. Implemented over System.IO at the boundary and faked in tests.
/// </summary>
public interface ITerminalFragment
{
    /// <summary>The fragment file's full path.</summary>
    string Location { get; }

    bool Exists { get; }

    Task WriteAsync(TerminalProfiles profiles, CancellationToken cancellationToken);

    /// <summary>Deletes Vantage's fragment folder and nothing else; false when there was nothing to delete.</summary>
    Task<bool> RemoveAsync(CancellationToken cancellationToken);
}

/// <summary>What Claude Code's settings file says about <c>preferredNotifChannel</c>.</summary>
public abstract record NotificationChannelState
{
    /// <summary>There is no settings file.</summary>
    public sealed record Missing : NotificationChannelState;

    /// <summary>The file has no such setting.</summary>
    public sealed record Unset : NotificationChannelState;

    public sealed record Configured(string Value) : NotificationChannelState;

    /// <summary>The file exists but is not a JSON object; the reason is the parser's, shown only to report what failed.</summary>
    public sealed record Unreadable(string Reason) : NotificationChannelState;
}

/// <summary>The one setting Vantage reads and writes in <c>~\.claude\settings.json</c>.</summary>
public interface IClaudeSettings
{
    /// <summary>The settings file's full path.</summary>
    string Location { get; }

    Task<NotificationChannelState> ReadNotificationChannelAsync(CancellationToken cancellationToken);

    /// <summary>Sets <c>preferredNotifChannel</c> to <c>terminal_bell</c>, keeping every other setting.</summary>
    Task EnableTerminalBellAsync(CancellationToken cancellationToken);
}