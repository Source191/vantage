namespace Source191.Torni.Ports;

/// <summary>What Claude Code's settings file says about <c>preferredNotifChannel</c>.</summary>
public abstract record NotificationChannelState
{
    /// <summary>The <c>preferredNotifChannel</c> value that makes Claude Code ring the terminal bell.</summary>
    public const string TerminalBell = "terminal_bell";

    /// <summary>There is no settings file.</summary>
    public sealed record Missing : NotificationChannelState;

    /// <summary>The file has no such setting.</summary>
    public sealed record Unset : NotificationChannelState;

    public sealed record Configured(string Value) : NotificationChannelState;

    /// <summary>The file exists but is not a JSON object; the reason is the parser's, shown only to report what failed.</summary>
    public sealed record Unreadable(string Reason) : NotificationChannelState;
}