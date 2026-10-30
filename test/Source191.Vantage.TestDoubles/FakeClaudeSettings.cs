using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>An in-memory settings file: enabling the bell turns the state into Configured("terminal_bell").</summary>
public sealed class FakeClaudeSettings(NotificationChannelState state) : IClaudeSettings
{
    public FakeClaudeSettings()
        : this(new NotificationChannelState.Missing())
    {
    }

    public string Location => @"C:\Users\me\.claude\settings.json";

    public NotificationChannelState State { get; private set; } = state;

    public int Enabled { get; private set; }

    /// <summary>When set, enabling the bell throws it.</summary>
    public IOException? FailTheWriteWith { get; set; }

    /// <summary>When set, reading the channel throws it.</summary>
    public IOException? FailTheReadWith { get; set; }

    public Task<NotificationChannelState> ReadNotificationChannelAsync(CancellationToken cancellationToken)
    {
        if (FailTheReadWith is not null)
        {
            throw FailTheReadWith;
        }

        return Task.FromResult(State);
    }

    public Task EnableTerminalBellAsync(CancellationToken cancellationToken)
    {
        if (FailTheWriteWith is not null)
        {
            throw FailTheWriteWith;
        }

        Enabled++;
        State = new NotificationChannelState.Configured("terminal_bell");

        return Task.CompletedTask;
    }
}