namespace Source191.Vantage.Domain;

/// <summary>
/// A Windows Terminal tab that Vantage opened. Pending until the tab's shell claims it with its own process.
/// </summary>
public sealed record TabRegistration(
    Guid Id,
    string SessionId,
    TabColour Colour,
    DateTimeOffset CreatedAt,
    int? ProcessId,
    DateTimeOffset? ProcessStartedAt)
{
    private static readonly TimeSpan PendingGracePeriod = TimeSpan.FromSeconds(60);

    public static TabRegistration Pending(string sessionId, TabColour colour, DateTimeOffset createdAt)
    {
        return new TabRegistration(Guid.NewGuid(), sessionId, colour, createdAt, ProcessId: null, ProcessStartedAt: null);
    }

    public TabRegistration ClaimedBy(int processId, DateTimeOffset processStartedAt)
    {
        return this with { ProcessId = processId, ProcessStartedAt = processStartedAt };
    }

    public bool IsLive(DateTimeOffset now, IProcessRegistry processes)
    {
        if (ProcessId is null)
        {
            return now - CreatedAt < PendingGracePeriod;
        }

        return processes.StartTimeOf(ProcessId.Value) == ProcessStartedAt;
    }
}

public sealed class TabRegistrations(IReadOnlyList<TabRegistration> items)
{
    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes)
    {
        return [.. items.Where(registration => registration.IsLive(now, processes))];
    }
}