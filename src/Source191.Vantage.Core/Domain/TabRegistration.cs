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

    public static TabRegistration Pending(string sessionId, TabColour colour, DateTimeOffset createdAt) => new TabRegistration(Guid.NewGuid(), sessionId, colour, createdAt, ProcessId: null, ProcessStartedAt: null);

    public TabRegistration ClaimedBy(int processId, DateTimeOffset processStartedAt) => this with { ProcessId = processId, ProcessStartedAt = processStartedAt };

    /// <summary>
    /// The Windows Terminal process whose window holds this tab, found by walking up from the tab's shell. Null for a
    /// pending registration or when the chain does not reach Windows Terminal within a few steps.
    /// </summary>
    public int? OwningTerminal(IProcessRegistry processes)
    {
        int? current = ProcessId;
        for (int step = 0; step < 5 && current is not null; step++)
        {
            ProcessParent? parent = processes.ParentOf(current.Value);
            if (parent?.Name == "WindowsTerminal")
            {
                return parent.ProcessId;
            }

            current = parent?.ProcessId;
        }

        return null;
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
    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes) => [.. items.Where(registration => registration.IsLive(now, processes))];
}