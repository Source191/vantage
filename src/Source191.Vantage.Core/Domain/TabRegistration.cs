namespace Source191.Vantage.Domain;

/// <summary>
/// A Windows Terminal tab that Vantage opened. Pending until the tab's shell claims it with its own process.
/// </summary>
public sealed record TabRegistration(
    Guid Id,
    TabOwner Owner,
    TabColour Colour,
    DateTimeOffset CreatedAt,
    int? ProcessId,
    DateTimeOffset? ProcessStartedAt)
{
    private static readonly TimeSpan PendingGracePeriod = TimeSpan.FromSeconds(60);

    public static TabRegistration Pending(TabOwner owner, TabColour colour, DateTimeOffset createdAt) => new TabRegistration(Guid.NewGuid(), owner, colour, createdAt, ProcessId: null, ProcessStartedAt: null);

    public TabRegistration ClaimedBy(int processId, DateTimeOffset processStartedAt) => this with { ProcessId = processId, ProcessStartedAt = processStartedAt };

    /// <summary>
    /// The Windows Terminal process whose window holds this tab, found by walking up from the tab's shell. Null for a
    /// pending registration or when the chain does not reach Windows Terminal within a few steps.
    /// </summary>
    public int? OwningTerminal(IProcessRegistry processes) => ProcessId is int shell ? TerminalTab.TerminalOf(shell, processes) : null;


    public bool IsLive(DateTimeOffset now, IProcessRegistry processes)
    {
        if (ProcessId is null)
        {
            return now - CreatedAt < PendingGracePeriod;
        }

        return processes.StartTimeOf(ProcessId.Value) == ProcessStartedAt;
    }
}

/// <summary>Whose tab a registration is: a session's, or Vantage's own.</summary>
public abstract record TabOwner
{
    public static readonly TabOwner Vantage = new ForVantage();

    public sealed record ForSession(SessionId Id) : TabOwner;

    /// <summary>The tab Vantage itself runs in, so its colour is not handed to a session tab.</summary>
    public sealed record ForVantage : TabOwner;
}

public sealed class TabRegistrations(IReadOnlyList<TabRegistration> items)
{
    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes) => [.. items.Where(registration => registration.IsLive(now, processes))];
}