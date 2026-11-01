namespace Source191.Vantage.Ports;

/// <summary>The one setting Vantage reads and writes in <c>~\.claude\settings.json</c>.</summary>
public interface IClaudeSettings
{
    /// <summary>The settings file's full path.</summary>
    string Location { get; }

    Task<NotificationChannelState> ReadNotificationChannelAsync(CancellationToken cancellationToken);

    /// <summary>Sets <c>preferredNotifChannel</c> to <c>terminal_bell</c>, keeping every other setting.</summary>
    Task EnableTerminalBellAsync(CancellationToken cancellationToken);
}